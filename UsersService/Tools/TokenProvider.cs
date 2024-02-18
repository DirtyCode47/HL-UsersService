using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using UsersService.Entities;

namespace UsersService.Tools
{
    public class TokenProvider:ITokenProvider
    {
        private readonly IConfiguration _configuration;
        public TokenProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string GenerateAccessToken(User user, AuthInfo authInfo)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim("jwtId", authInfo.jwtId.ToString()),
                new Claim(ClaimTypes.Name, authInfo.login),
                new Claim(ClaimTypes.Role, Convert.ToString(authInfo.role)),
            };
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                _configuration.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }

        public string GenerateRefreshToken()
        {
            var refreshTokenSize = 64; 
            var refreshTokenBytes = new byte[refreshTokenSize];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(refreshTokenBytes);
            }

            return Convert.ToBase64String(refreshTokenBytes);
        }
    }
}
