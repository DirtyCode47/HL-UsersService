using System.Security.Cryptography;

namespace UsersService.Services
{
    public class AuthService
    {
        public byte[] CreatePasswordHash(string password, byte[] salt)
        {
            byte[] hash = null;
            using (var hmac = new HMACSHA256(salt))
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
    }
}
