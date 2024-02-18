using System.Security.Cryptography;

namespace UsersService.Tools
{
    public interface ISecurityManager
    {
        public byte[] CreateHash(string mainWord, byte[] salt);
        public byte[] GenerateSalt(int length);
        public bool VerifyHash(string mainWord, byte[] mainWordHash, byte[] mainWordSalt);
    }
}
