
using Grpc.Core;
using UsersService.Protos;
using User = UsersService.Entities.User;
using UsersService.Cache;
using static Grpc.Core.Metadata;
using Microsoft.Extensions.Hosting;
using System.Security.Principal;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Configuration;
using Google.Protobuf.Collections;
using UsersService.Entities;
using StackExchange.Redis;
using UsersService.Tools;
using UsersService.Repository.Auth;
using UsersService.Repository.Users;


namespace UsersService.Services
{
    public class UsersServiceImplementation : Protos.UsersService.UsersServiceBase
    {
        private readonly IUsersRepository _usersRepository;
        private readonly IAuthRepository _authRepository;
        private readonly ICacheService _cacheService;
        private readonly ISecurityManager _securityManager;
        private readonly ITokenProvider _tokenProvider;
        private readonly IConfiguration _configuration;
        public UsersServiceImplementation(IUsersRepository usersRepository, ICacheService cacheService, ISecurityManager securityManager, ITokenProvider tokenProvider, IConfiguration configuration, IAuthRepository authRepository)
        {
            _authRepository = authRepository;
            _usersRepository = usersRepository;
            _cacheService = cacheService;
            _securityManager = securityManager;
            _tokenProvider = tokenProvider;
            _configuration = configuration;
        }

        public override async Task<CreateUserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
        {
            //Guid user_id = Guid.Parse(request.User.Id);
            Guid user_id = Guid.NewGuid();

            if (await _usersRepository.GetAsync(user_id) != null)
                throw new RpcException(new Status(StatusCode.AlreadyExists, "Record with this id already exists"));

            //if (await _usersRepository.FindByPostCode(request.User.PostCode) != null)
            //    throw new RpcException(new Status(StatusCode.AlreadyExists, "Record with this post code already exists"));

            var user = new User()
            {
                id = user_id,
                postCode = request.User.PostCode,
                firstName = request.User.FirstName,
                middleName = request.User.MiddleName,
                lastName = request.User.LastName,
                phone = request.User.Phone,
            };

            User added_user = await _usersRepository.CreateAsync(user);


            byte[] password_salt = _securityManager.GenerateSalt(16);
            byte[] password_hash = _securityManager.CreateHash(request.User.Password, password_salt);

            var auth_info = new AuthInfo()
            {
                id = user_id,
                role = request.User.Role,
                login = request.User.Login,
                passwordHash = password_hash,
                passwordSalt = password_salt
            };

            AuthInfo added_auth_info = await _authRepository.CreateAsync(auth_info);
            
            await _usersRepository.CompleteAsync();
            await _authRepository.CompleteAsync();
           

            return new CreateUserResponse
            {
                User = request.User
            };
        }

        public override async Task<DeleteUserResponse> DeleteUser(DeleteUserRequest request, ServerCallContext context)
        {

            if (!Guid.TryParse(request.Id, out Guid user_id))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Not correct format of id"));
            }

            User user = await _usersRepository.GetAsync(user_id);
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Can't find record in Db with this id"));
            }

            AuthInfo authInfo = await _authRepository.GetAsync(user_id);
            _cacheService.AddOrUpdateCache($"blacklist:{authInfo.jwtId}", authInfo.jwtId); //Добавляем в черный лист

            await _usersRepository.Delete(user_id);
            await _usersRepository.CompleteAsync();
            // Удаляем из кэша

            return new DeleteUserResponse
            {
                User = new Protos.UserDTO
                {
                    Id = user.id.ToString(),
                    PostCode = user.postCode,
                    FirstName = user.firstName,
                    MiddleName = user.middleName,
                    LastName = user.lastName,
                    Phone = user.phone
                }
            };
        }

        public override async Task<UpdateUserResponse> UpdateUser(UpdateUserRequest request, ServerCallContext context)
        {
            if (!Guid.TryParse(request.User.Id, out Guid user_id))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Not correct format of id"));
            }

            var existingUser = await _usersRepository.GetAsync(user_id);

            if (existingUser == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Can't find a record in the database with this id"));
            }

            //if (await _usersRepository.FindByPostCode(request.User.PostCode) != null)
            //{
            //    throw new RpcException(new Status(StatusCode.AlreadyExists, "Record with this post code already exists"));
            //}

            byte[] password_salt = _securityManager.GenerateSalt(16);
            byte[] password_hash = _securityManager.CreateHash(request.User.Password, password_salt);

            existingUser.postCode = request.User.PostCode;
            existingUser.firstName = request.User.FirstName;
            existingUser.middleName = request.User.MiddleName;
            existingUser.lastName = request.User.LastName;
            existingUser.phone = request.User.Phone;


            var userAuthInfo = await _authRepository.GetAsync(user_id);
            userAuthInfo.role = request.User.Role;
            userAuthInfo.login = request.User.Login;
            userAuthInfo.passwordHash = password_hash;
            userAuthInfo.passwordSalt = password_salt;

            _cacheService.AddOrUpdateCache($"blacklist:{userAuthInfo.jwtId}", userAuthInfo.jwtId); //Добавляем в черный лист


            var entry = _usersRepository.Update(existingUser);
            if (entry == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Can't find record in Db with this id"));

            _authRepository.Update(userAuthInfo);

            await _usersRepository.CompleteAsync();
            await _authRepository.CompleteAsync();

            return new UpdateUserResponse
            {
                User = new Protos.UserDTO
                {
                    Id = existingUser.id.ToString(),
                    PostCode = existingUser.postCode,
                    FirstName = existingUser.firstName,
                    MiddleName = existingUser.middleName,
                    LastName = existingUser.lastName,
                    Phone = existingUser.phone
                }
            };
        }

        public override async Task<GetUserResponse> GetUser(GetUserRequest request, ServerCallContext context)
        {
            if(!Guid.TryParse(request.Id, out Guid guid))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Not correct format of id"));
            }

            var user = await _usersRepository.GetAsync(guid);

            if (user == null)
               throw new RpcException(new Status(StatusCode.NotFound, "Can't find a record in the database with this id"));

            
            return new GetUserResponse
            {
                User = new Protos.UserDTO
                {
                    Id = user.id.ToString(),
                    PostCode = user.postCode,
                    FirstName = user.firstName,
                    MiddleName = user.middleName,
                    LastName = user.lastName,
                    Phone = user.phone,
                }
            };
        }

        public override async Task<GetAllUsersResponse> GetAllUsers(GetAllUsersRequest request, ServerCallContext context)
        {
            var users = await _usersRepository.GetAllUsersAsync(); //Надо не забыть добавить в кэш
            
            var response = new GetAllUsersResponse();

            foreach (var user in users)
            {
                response.Users.Add(new UserDTO
                {
                    Id = user.id.ToString(),
                    PostCode = user.postCode,
                    FirstName = user.firstName,
                    MiddleName = user.middleName,
                    LastName = user.lastName,
                    Phone = user.phone
                });
            }

            return response;
        }

        //public override Task<FindUsersWithFiltersResponse> FindUsersWithFilters(FindUsersWithFiltersRequest request, ServerCallContext context)
        //{
        //    var users = _usersRepository.FindUsersWithFilters(request.Name,request.Role,request.PostCode);
        //    if (!users.Any()) throw new RpcException(new Status(StatusCode.InvalidArgument, "Can't find elements in page"));

        //    FindUsersWithFiltersResponse findWithFiltersResponse = new();

        //    foreach (var user in users)
        //    {
        //        findWithFiltersResponse.Users.Add(new Protos.UserDTO
        //        {
        //            Id = user.id.ToString(),
        //            PostCode = user.postCode,
        //            FirstName = user.firstName,
        //            MiddleName = user.middleName,
        //            LastName = user.lastName,
        //            Phone = user.phone,
        //        });
        //    }

        //    return Task.FromResult(findWithFiltersResponse);
        //}

    }
}
