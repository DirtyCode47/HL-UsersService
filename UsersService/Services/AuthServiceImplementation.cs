﻿using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using UsersService.Cache;
using UsersService.Protos;
using UsersService.Entities;
using Newtonsoft.Json;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Runtime.CompilerServices;
using UsersService.Tools;
using UsersService.Repository.Auth;
using UsersService.Repository.Users;

namespace UsersService.Services
{
    public class AuthServiceImplementation:Protos.AuthService.AuthServiceBase
    {
        private readonly IUsersRepository _usersRepository;
        private readonly IAuthRepository _authRepository;
        private readonly ICacheService _cacheService;
        private readonly ISecurityManager _securityManager;
        private readonly ITokenProvider _tokenProvider;
        private readonly IConfiguration _configuration;
        public AuthServiceImplementation(IUsersRepository usersRepository, ICacheService cacheService, ISecurityManager securityManager, ITokenProvider tokenProvider, IConfiguration configuration, IAuthRepository authRepository)
        {
            _authRepository = authRepository;
            _usersRepository = usersRepository;
            _cacheService = cacheService;
            _securityManager = securityManager;
            _tokenProvider = tokenProvider;
            _configuration = configuration;
        }
        public override async Task<LoginUserResponse> LoginUser(LoginUserRequest request, ServerCallContext context)
        {
            AuthInfo? authInfo = _authRepository.GetAuthInfoByLogin(request.Login);
            
            if (authInfo is null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Can't find user with such login!"));
            }

            if (!_securityManager.VerifyHash(request.Password, authInfo.passwordHash, authInfo.passwordSalt))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Password is not correct!"));
            }

            User? user = await _usersRepository.GetAsync(authInfo.id);

            authInfo.jwtId = Guid.NewGuid();

            string accessToken = _tokenProvider.GenerateAccessToken(user, authInfo);
            string refreshToken = _tokenProvider.GenerateRefreshToken();

            byte[] refreshTokenSalt = _securityManager.GenerateSalt(16);
            byte[] refreshTokenHash = _securityManager.CreateHash(refreshToken, refreshTokenSalt);

            authInfo.refreshTokenHash = refreshTokenHash;
            authInfo.refreshTokenSalt = refreshTokenSalt;
            authInfo.refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            _authRepository.Update(authInfo);
            _authRepository.CompleteAsync();

            return new LoginUserResponse() { AccessToken = accessToken, RefreshToken = refreshToken };

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
                    ValidateLifetime = false, 
                    ClockSkew = TimeSpan.Zero
                };

                SecurityToken validatedToken;
                var principal = tokenHandler.ValidateToken(request.AccessToken, tokenValidationParameters, out validatedToken);


                var handler = new JwtSecurityTokenHandler();  
                var jsonToken = handler.ReadToken(request.AccessToken) as JwtSecurityToken;

                var jwt_id = jsonToken.Claims.FirstOrDefault(c => c.Type == "jwtId").Value;


                if (_cacheService.GetFromCache<string>($"blacklist:{jwt_id}") != null)
                {
                    return Task.FromResult(new ValidateAccessTokenResponse() { Success = false });
                }

                return Task.FromResult(new ValidateAccessTokenResponse() { Success = true });
            }
            catch (Exception ex)
            {
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
                    ValidateLifetime = true, 
                    ClockSkew = TimeSpan.Zero 
                };

                SecurityToken validatedToken;
                var principal = tokenHandler.ValidateToken(request.AccessToken, tokenValidationParameters, out validatedToken);

                return Task.FromResult(new ValidateAccessTokenLifetimeResponse() { Success = true });
            }
            catch (SecurityTokenExpiredException)
            {
                return Task.FromResult(new ValidateAccessTokenLifetimeResponse() { Success = false });
            }
            catch (Exception)
            {
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

            ValidateRefreshTokenResponse response = (authInfo.refreshTokenExpiry > DateTime.UtcNow && _securityManager.VerifyHash(request.RefreshToken,authInfo.refreshTokenHash,authInfo.refreshTokenSalt)) ?
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
            _cacheService.AddOrUpdateCache($"blacklist:{old_jwt_id}", old_jwt_id); 
            
            User? user = await _usersRepository.GetAsync(Guid.Parse(request.UserId));
            AuthInfo? authInfo = await _authRepository.GetAsync(Guid.Parse(request.UserId));

            authInfo.jwtId = Guid.NewGuid();

            string accessToken = _tokenProvider.GenerateAccessToken(user,authInfo);
            string refreshToken = _tokenProvider.GenerateRefreshToken();

            byte[] refreshTokenSalt = _securityManager.GenerateSalt(16);
            byte[] refreshTokenHash = _securityManager.CreateHash(refreshToken, refreshTokenSalt);

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
            _cacheService.AddOrUpdateCache($"blacklist:{old_jwt_id}", old_jwt_id); 

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
