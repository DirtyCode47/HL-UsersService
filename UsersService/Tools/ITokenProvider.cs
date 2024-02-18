using UsersService.Entities;

namespace UsersService.Tools
{
    public interface ITokenProvider
    {
        public string GenerateAccessToken(User user, AuthInfo authInfo);
        public string GenerateRefreshToken();
    }
}
