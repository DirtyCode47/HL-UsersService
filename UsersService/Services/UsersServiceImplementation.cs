﻿
using Grpc.Core;
using UsersService.Repository;
using UsersService.Protos;
using User = UsersService.Entities.User;
using UsersService.Cache;
using static Grpc.Core.Metadata;
using Microsoft.Extensions.Hosting;
using UsersService.Entities;
using System.Security.Principal;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Configuration;


namespace UsersService.Services
{
    public class UsersServiceImplementation : Protos.UsersService.UsersServiceBase
    {
        private readonly UsersRepository _usersRepository;
        private readonly CacheService _cacheService;
        private readonly SecurityService _securityService;
        private readonly IConfiguration _configuration;
        public UsersServiceImplementation(UsersRepository usersRepository, CacheService cacheService, SecurityService authService, IConfiguration configuration)
        {
            _usersRepository = usersRepository ?? throw new ArgumentNullException(nameof(usersRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _securityService = authService ?? throw new ArgumentNullException(nameof(authService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public override async Task<CreateUserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
        {
            //Guid user_id = Guid.Parse(request.User.Id);
            Guid user_id = Guid.NewGuid();

            if (await _usersRepository.GetUserAsync(user_id) != null)
                throw new RpcException(new Status(StatusCode.AlreadyExists, "Record with this id already exists"));

            if(await _usersRepository.FindByPostCode(request.User.PostCode) != null)
                throw new RpcException(new Status(StatusCode.AlreadyExists, "Record with this post code already exists"));

            byte[] password_salt = _securityService.GenerateRandomSalt(16);
            byte[] password_hash = _securityService.CreatePasswordHash(request.User.Password, password_salt);

            User user = new User()
            {
                id = user_id,
                role = request.User.Role,
                post_code = request.User.PostCode,
                first_name = request.User.FirstName,
                middle_name = request.User.MiddleName,
                last_name = request.User.LastName,
                phone = request.User.Phone,
                login = request.User.Login,
                PasswordHash = password_hash,
                PasswordSalt = password_salt,
                JwtId = Guid.NewGuid()
            };

            User added_user = await _usersRepository.CreateUserAsync(user);
            await _usersRepository.CompleteAsync();

            // Обновляем кэш
            _cacheService.AddOrUpdateCache($"user:{added_user.id}", added_user);

            return new CreateUserResponse
            {
                User = request.User
            };
        }

        public override async Task<DeleteUserResponse> DeleteUser(DeleteUserRequest request, ServerCallContext context)
        {
            Guid user_id = Guid.Parse(request.Id);

            User user = await _usersRepository.GetUserAsync(user_id);
            if (user == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Can't find record in Db with this id"));

            _usersRepository.DeleteUser(user_id);
            await _usersRepository.CompleteAsync();

            // Удаляем из кэша
            _cacheService.ClearCache($"user:{user.id}");

            return new DeleteUserResponse
            {
                User = new Protos.UserDTO
                {
                    Id = user.id.ToString(),
                    Role = user.role,
                    PostCode = user.post_code,
                    FirstName = user.first_name,
                    MiddleName = user.middle_name,
                    LastName = user.last_name,
                    Phone = user.phone,
                    Login = user.login,
                }
            };
        }

        public override async Task<UpdateUserResponse> UpdateUser(UpdateUserRequest request, ServerCallContext context)
        {
            Guid postId = Guid.Parse(request.User.Id);
            
            var existingUser = await _usersRepository.GetUserAsync(postId);

            if (existingUser == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Can't find a record in the database with this id"));
            }

            if (await _usersRepository.FindByPostCode(request.User.PostCode) != null)
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, "Record with this post code already exists"));
            }

            byte[] password_salt = _securityService.GenerateRandomSalt(16);
            byte[] password_hash = _securityService.CreatePasswordHash(request.User.Password, password_salt);

            existingUser.role = request.User.Role;
            existingUser.post_code = request.User.PostCode;
            existingUser.first_name = request.User.FirstName;
            existingUser.middle_name = request.User.MiddleName;
            existingUser.last_name = request.User.LastName;
            existingUser.phone = request.User.Phone;
            existingUser.PasswordHash = password_hash;
            existingUser.PasswordSalt = password_salt;

            var entry = _usersRepository.UpdateUser(existingUser);
            if (entry == null) 
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Can't find record in Db with this id"));

            await _usersRepository.CompleteAsync();

            // Обновить кэш
            _cacheService.AddOrUpdateCache($"user:{entry.id}", entry);

            return new UpdateUserResponse
            {
                User = new Protos.UserDTO
                {
                    Id = existingUser.id.ToString(),
                    Role = existingUser.role,
                    PostCode = existingUser.post_code,
                    FirstName = existingUser.first_name,
                    MiddleName = existingUser.middle_name,
                    LastName = existingUser.last_name,
                    Phone = existingUser.phone
                }
            };
        }

        public override async Task<GetUserResponse> GetUser(GetUserRequest request, ServerCallContext context)
        {
            Guid guid = Guid.Parse(request.Id);

            User user = _cacheService.GetFromCache<User>($"user:{guid}");
            if (user == null)
            {
                // Если записи нет в кэше, пытаемся получить из базы данных
                user = await _usersRepository.GetUserAsync(guid);

                if (user == null)
                    throw new RpcException(new Status(StatusCode.NotFound, "Can't find a record in the database with this id"));

                // добавляем в кэш
                _cacheService.AddOrUpdateCache($"user:{user.id}", user);
            }

            

            return new GetUserResponse
            {
                User = new Protos.UserDTO
                {
                    Id = user.id.ToString(),
                    Role = user.role,
                    PostCode = user.post_code,
                    FirstName = user.first_name,
                    MiddleName = user.middle_name,
                    LastName = user.last_name,
                    Phone = user.phone,
                }
            };
        }

        public override Task<FindUsersWithFiltersResponse> FindUsersWithFilters(FindUsersWithFiltersRequest request, ServerCallContext context)
        {
            var users = _usersRepository.FindUsersWithFilters(request.Name,request.Role,request.PostCode);
            if (!users.Any()) throw new RpcException(new Status(StatusCode.InvalidArgument, "Can't find elements in page"));

            FindUsersWithFiltersResponse findWithFiltersResponse = new();

            foreach (var user in users)
            {
                findWithFiltersResponse.Users.Add(new Protos.UserDTO
                {
                    Id = user.id.ToString(),
                    Role = user.role,
                    PostCode = user.post_code,
                    FirstName = user.first_name,
                    MiddleName = user.middle_name,
                    LastName = user.last_name,
                    Phone = user.phone,
                });
            }

            return Task.FromResult(findWithFiltersResponse);
        }

    }
}
