using System.Security.Cryptography;

namespace UsersService.Tools
{
    public class SecurityManager:ISecurityManager
    {
        public byte[] CreateHash(string mainWord, byte[] salt)
        {
            byte[] hash = null;
            using (var hmac = new HMACSHA512(salt))
            {
                hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(mainWord));
            }
            return hash;
        }

        public byte[] GenerateSalt(int length)
        {
            // Используем статический метод RandomNumberGenerator.GetBytes для генерации случайной соли
            byte[] salt = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
        public bool VerifyHash(string mainWord, byte[] mainWordHash, byte[] mainWordSalt)
        {
            using (var hmac = new HMACSHA512(mainWordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(mainWord));
                return computedHash.SequenceEqual(mainWordHash);
            }
        }
    }
}
