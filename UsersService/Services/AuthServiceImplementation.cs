using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using UsersService.Cache;
using UsersService.Protos;
using UsersService.Repository;
using UsersService.Entities;

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

                // Если успешно прошли основную проверку, валидация считается успешной
                return Task.FromResult(new ValidateAccessTokenResponse() { Success = true });
            }
            catch (Exception ex)
            {
                // В случае ошибки валидации, возвращаем false
                return Task.FromResult(new ValidateAccessTokenResponse() { Success = false });
            }
        }



        //public override Task<ValidateTokenLifetimeResponse> ValidateTokenLifetime(ValidateTokenLifetimeRequest request, ServerCallContext context)
        //{
        //    try
        //    {

        //        var tokenHandler = new JwtSecurityTokenHandler();
        //        var access_token = tokenHandler.ReadToken(request.AccessToken) as JwtSecurityToken;




        //        if (access_token == null)
        //        {
        //            return Task.FromResult(new ValidateTokenLifetimeResponse() { Success = false, AccessToken = "", RefreshToken = "" });
        //        }

        //        bool success = (access_token.ValidTo > DateTime.UtcNow) ? true : false;

        //        if (!success) 
        //        {
        //            RefreshToken refreshTokenData = RefreshToken.FromJson(request.RefreshToken);
        //            if
        //        }


        //            return success;

        //    }
        //    catch (Exception ex)
        //    {

        //        return false;
        //    }
        //}
    }
}
