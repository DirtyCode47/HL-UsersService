using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using UsersService.Cache;
using UsersService.Protos;
using UsersService.Repository;
using UsersService.Entities;
using Newtonsoft.Json;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Runtime.CompilerServices;

namespace UsersService.Services
{
    public class AuthServiceImplementation:Protos.AuthService.AuthServiceBase
    {
        private readonly AuthRepository _authRepository;
        private readonly UsersRepository _usersRepository;
        private readonly CacheService _cacheService;
        private readonly SecurityService _securityService;
        private readonly IConfiguration _configuration;
        public AuthServiceImplementation(UsersRepository usersRepository, CacheService cacheService, SecurityService authService, IConfiguration configuration, AuthRepository authRepository)
        {
            _usersRepository = usersRepository ?? throw new ArgumentNullException(nameof(usersRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _securityService = authService ?? throw new ArgumentNullException(nameof(authService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
        }
        public override async Task<LoginUserResponse> LoginUser(LoginUserRequest request, ServerCallContext context)
        {
            AuthInfo? authInfo = _authRepository.GetAuthInfoByLogin(request.Login);
            
            //AuthInfo? authInfo = await _authRepository.GetAuthInfoByLogin(request.Login);
            

            if (authInfo is null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Can't find user with such login!"));
            }

            if (!_securityService.VerifyHash(request.Password, authInfo.passwordHash, authInfo.passwordSalt))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Password is not correct!"));
            }

            User? user = await _usersRepository.GetAsync(authInfo.id);

            authInfo.jwtId = Guid.NewGuid();

            string accessToken = _securityService.CreateToken(user, authInfo);
            string refreshToken = _securityService.GenerateRefreshToken();

            byte[] refreshTokenSalt = _securityService.GenerateSalt(16);
            byte[] refreshTokenHash = _securityService.CreateHash(refreshToken, refreshTokenSalt);

            authInfo.refreshTokenHash = refreshTokenHash;
            authInfo.refreshTokenSalt = refreshTokenSalt;
            authInfo.refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            _authRepository.Update(authInfo);
            _authRepository.CompleteAsync();

            return new LoginUserResponse() { AccessToken = accessToken, RefreshToken = refreshToken };


            //return null;
        }

        public override Task<ValidateAccessTokenResponse> ValidateAccessToken(ValidateAccessTokenRequest request, ServerCallContext context)
        {
            if (request.AccessToken == null)
            {
                return Task.FromResult(new ValidateAccessTokenResponse() { Success = false });
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value);

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false, // Отключаем проверку времени жизни
                    ClockSkew = TimeSpan.Zero
                };

                SecurityToken validatedToken;
                var principal = tokenHandler.ValidateToken(request.AccessToken, tokenValidationParameters, out validatedToken);


                var handler = new JwtSecurityTokenHandler();  //Проверка на то, есть ли токен в блэклисте
                var jsonToken = handler.ReadToken(request.AccessToken) as JwtSecurityToken;

                var jwt_id = jsonToken.Claims.FirstOrDefault(c => c.Type == "jwtId").Value;


                if (_cacheService.GetFromCache<string>($"blacklist:{jwt_id}") != null)
                {
                    return Task.FromResult(new ValidateAccessTokenResponse() { Success = false });
                }

                // Если успешно прошли основную проверку, валидация считается успешной
                return Task.FromResult(new ValidateAccessTokenResponse() { Success = true });
            }
            catch (Exception ex)
            {
                // В случае ошибки валидации, возвращаем false
                return Task.FromResult(new ValidateAccessTokenResponse() { Success = false });
            }
        }



        public override Task<ValidateAccessTokenLifetimeResponse> ValidateAccessTokenLifetime(ValidateAccessTokenLifetimeRequest request, ServerCallContext context)
        {
            if (request.AccessToken == null)
            {
                return Task.FromResult(new ValidateAccessTokenLifetimeResponse() { Success = false });
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value); 

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true, // Включаем проверку времени жизни
                    ClockSkew = TimeSpan.Zero // Устанавливаем отсутствие дополнительного времени (ClockSkew) для точной проверки
                };

                SecurityToken validatedToken;
                var principal = tokenHandler.ValidateToken(request.AccessToken, tokenValidationParameters, out validatedToken);

                // Если успешно прошли валидацию времени жизни, валидация считается успешной
                return Task.FromResult(new ValidateAccessTokenLifetimeResponse() { Success = true });
            }
            catch (SecurityTokenExpiredException)
            {
                // Токен истек и срок его действия завершен
                return Task.FromResult(new ValidateAccessTokenLifetimeResponse() { Success = false });
            }
            catch (Exception)
            {
                // В случае других ошибок валидации, считаем токен невалидным
                return Task.FromResult(new ValidateAccessTokenLifetimeResponse() { Success = false });
            }
        }


        public override async Task<ValidateRefreshTokenResponse> ValidateRefreshToken(ValidateRefreshTokenRequest request, ServerCallContext context)
        {
            if (!Guid.TryParse(request.UserId, out Guid userId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Not correct format of id"));
            }

            var authInfo = await _authRepository.GetAsync(userId);

            if(authInfo == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Can't find a record in the database with this id"));
            }

            ValidateRefreshTokenResponse response = (authInfo.refreshTokenExpiry > DateTime.UtcNow && _securityService.VerifyHash(request.RefreshToken,authInfo.refreshTokenHash,authInfo.refreshTokenSalt)) ?
                new ValidateRefreshTokenResponse() { Success = true } :
                new ValidateRefreshTokenResponse() { Success = false };

            return response;
        }

        public override async Task<RegenerateTokensResponse> RegenerateTokens(RegenerateTokensRequest request, ServerCallContext context)
        {
            string jwtToken = request.OldAccessToken;
            
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var old_jwt_id = jsonToken.Claims.FirstOrDefault(c => c.Type == "jwtId").Value;
            _cacheService.AddOrUpdateCache($"blacklist:{old_jwt_id}", old_jwt_id); //Добавляем в черный лист
            
            User? user = await _usersRepository.GetAsync(Guid.Parse(request.UserId));
            AuthInfo? authInfo = await _authRepository.GetAsync(Guid.Parse(request.UserId));

            authInfo.jwtId = Guid.NewGuid();

            string accessToken = _securityService.CreateToken(user,authInfo);
            string refreshToken = _securityService.GenerateRefreshToken();

            byte[] refreshTokenSalt = _securityService.GenerateSalt(16);
            byte[] refreshTokenHash = _securityService.CreateHash(refreshToken, refreshTokenSalt);

            authInfo.refreshTokenHash = refreshTokenHash;
            authInfo.refreshTokenSalt = refreshTokenSalt;
            authInfo.refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            _authRepository.Update(authInfo);
            await _authRepository.CompleteAsync();

            return new RegenerateTokensResponse() { AccessToken = accessToken, RefreshToken = refreshToken };
        }

        public override async Task<LogoutUserResponse> LogoutUser(LogoutUserRequest request, ServerCallContext context)
        {
            if (!Guid.TryParse(request.UserId, out Guid userId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Not correct format of id"));
            }

            var authInfo = await _authRepository.GetAsync(userId);

            if (authInfo == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Can't find a record in the database with this id"));
            }

            string jwtToken = request.AccessToken;

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var old_jwt_id = jsonToken.Claims.FirstOrDefault(c => c.Type == "jwtId").Value;
            _cacheService.AddOrUpdateCache($"blacklist:{old_jwt_id}", old_jwt_id); //Добавляем в черный лист

            authInfo.refreshTokenHash = null;
            authInfo.refreshTokenSalt = null;
            authInfo.refreshTokenExpiry = null;
            authInfo.jwtId = null;

            _authRepository.Update(authInfo);
            await _authRepository.CompleteAsync();

            return new LogoutUserResponse { AccessToken = jwtToken };
        }
    }
}
