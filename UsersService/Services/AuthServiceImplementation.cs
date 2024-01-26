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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UsersService.Services
{
    public class AuthServiceImplementation:Protos.AuthService.AuthServiceBase
    {
        private readonly UsersRepository _usersRepository;
        private readonly CacheService _cacheService;
        private readonly SecurityService _securityService;
        private readonly IConfiguration _configuration;
        public AuthServiceImplementation(UsersRepository usersRepository, CacheService cacheService, SecurityService authService, IConfiguration configuration)
        {
            _usersRepository = usersRepository ?? throw new ArgumentNullException(nameof(usersRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _securityService = authService ?? throw new ArgumentNullException(nameof(authService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        public override Task<LoginUserResponse> LoginUser(LoginUserRequest request, ServerCallContext context)
        {
            User? user = _usersRepository.GetUserByLogin(request.Login);

            if (user is null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Can't find user with such login!"));
            }

            if (!_securityService.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Password is not correct!"));
            }

            string access_token = _securityService.CreateToken(user);
            string refresh_token = _securityService.GenerateRefreshToken().ToJson();

            return Task.FromResult(new LoginUserResponse() { AccessToken = access_token, RefreshToken = refresh_token });
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

                var jwt_id = jsonToken.Claims.FirstOrDefault(c => c.Type == "JwtId");

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


        public override Task<ValidateRefreshTokenLifetimeResponse> ValidateRefreshTokenLifetime(ValidateRefreshTokenLifetimeRequest request, ServerCallContext context)
        {
            RefreshToken refreshToken = JsonConvert.DeserializeObject<RefreshToken>(request.RefreshToken);

            ValidateRefreshTokenLifetimeResponse response = (refreshToken.Expires < DateTime.UtcNow) ?
                new ValidateRefreshTokenLifetimeResponse() { Success = false } :
                new ValidateRefreshTokenLifetimeResponse() { Success = true };

            return Task.FromResult(response);
        }

        public override Task<RegenerateTokensResponse> RegenerateTokens(RegenerateTokensRequest request, ServerCallContext context)
        {
            string jwtToken = request.OldAccessToken; 

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var old_jwt_id = jsonToken.Claims.FirstOrDefault(c => c.Type == "JwtId");

            _cacheService.AddOrUpdateCache($"blacklist:{old_jwt_id.ToString()}", old_jwt_id.ToString()); //Добавляем в черный лист

            User user = _usersRepository.GetUser(Guid.Parse(request.UserId));

            user.JwtId = Guid.NewGuid();
            string access_token = _securityService.CreateToken(user);
            string refresh_token = _securityService.GenerateRefreshToken().ToJson();

            return Task.FromResult(new RegenerateTokensResponse() { AccessToken = access_token, RefreshToken = refresh_token });
        }

        //public override Task<LogoutUserResponse> LogoutUser(LogoutUserRequest request, ServerCallContext context)
        //{

        //}
    }
}
