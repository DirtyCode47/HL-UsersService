using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using UsersService.Entities;
using Newtonsoft.Json;

namespace UsersService.Services
{
    public class RefreshToken
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime Expires { get; set; }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static RefreshToken FromJson(string json)
        {
            return JsonConvert.DeserializeObject<RefreshToken>(json);
        }

        public bool IsExpired()
        {
            return DateTime.Now >= Expires;
        }
    }

    public class SecurityService
    {
        private readonly IConfiguration _configuration;
        public SecurityService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public byte[] CreatePasswordHash(string password, byte[] salt)
        {
            byte[] hash = null;
            using (var hmac = new HMACSHA512(salt))
            {
                hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
            return hash;
        }

        public byte[] GenerateRandomSalt(int length)
        {
            // Используем статический метод RandomNumberGenerator.GetBytes для генерации случайной соли
            byte[] salt = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
        public bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return computedHash.SequenceEqual(passwordHash);
            }
        }

        public string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.login),
                new Claim(ClaimTypes.Role, Convert.ToString(user.role))
            };
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                _configuration.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }

        public RefreshToken GenerateRefreshToken()
        {
            var refreshTokenSize = 64; // Размер RefreshToken (в байтах)
            var refreshTokenBytes = new byte[refreshTokenSize];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(refreshTokenBytes);
            }

            var refreshTokenData = new RefreshToken
            {
                Token = Convert.ToBase64String(refreshTokenBytes),
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7) // Например, устанавливаем срок жизни в 7 дней
            };

            return refreshTokenData;
        }
    }
}
