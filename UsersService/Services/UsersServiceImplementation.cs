
using Grpc.Core;
using UsersService.Repository;
using UsersService.Protos;
using User = UsersService.Entities.User;
using UsersService.Cache;
using static Grpc.Core.Metadata;
using Microsoft.Extensions.Hosting;
using UsersService.Entities;


namespace UsersService.Services
{
    public class UsersServiceImplementation : Protos.UsersService.UsersServiceBase
    {
        private readonly UsersRepository _usersRepository;
        private readonly CacheService _cacheService;
        private readonly AuthService _authService;
        public UsersServiceImplementation(UsersRepository usersRepository, CacheService cacheService, AuthService authService)
        {
            _usersRepository = usersRepository ?? throw new ArgumentNullException(nameof(usersRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        public override async Task<CreateUserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
        {
            Guid user_id = Guid.Parse(request.User.Id);

            byte[] password_salt = _authService.GenerateRandomSalt(16);
            byte[] password_hash = _authService.CreatePasswordHash(request.User.Password, password_salt);

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
                PasswordSalt = password_salt
            };

            if (await _usersRepository.GetUserAsync(user_id) != null)
                throw new RpcException(new Status(StatusCode.AlreadyExists, "This record already exist in Db"));

            User added_user = await _usersRepository.CreateUserAsync(user);
            await _usersRepository.CompleteAsync();

            // Обновляем кэш
            _cacheService.AddOrUpdateCache($"post:{added_user.id}", added_user);

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
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Can't find record in Db with this id"));

            _usersRepository.DeleteUser(user_id);
            await _usersRepository.CompleteAsync();

            // Удаляем из кэша
            _cacheService.ClearCache($"post:{user.id}");

            return new DeleteUserResponse
            {
                User = new Protos.User
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

        public override async Task<UpdateUserResponse> UpdateUser(UpdateUserRequest request, ServerCallContext context)
        {
            Guid postId = Guid.Parse(request.User.Id);

            // Найти существующую сущность по Id
            var existingUser = await _usersRepository.GetUserAsync(postId);

            if (existingUser == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Can't find a record in the database with this id"));
            }

            // Обновить свойства существующей сущности
            existingUser.role = request.User.Role;
            existingUser.post_code = request.User.PostCode;
            existingUser.first_name = request.User.FirstName;
            existingUser.middle_name = request.User.MiddleName;
            existingUser.last_name = request.User.LastName;
            existingUser.phone = request.User.Phone;

            var entry = _usersRepository.UpdateUser(existingUser);
            if (entry == null) 
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Can't find record in Db with this id"));

            await _usersRepository.CompleteAsync();

            // Обновить кэш
            _cacheService.AddOrUpdateCache($"post:{entry.id}", entry);

            return new UpdateUserResponse
            {
                User = new Protos.User
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

            User user = _cacheService.GetFromCache<User>($"post:{guid}");
            if (user == null)
            {
                // Если записи нет в кэше, пытаемся получить из базы данных
                user = await _usersRepository.GetUserAsync(guid);
                if (user == null)
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Can't find a record in the database with this id"));
            }

            // добавляем в кэш
            _cacheService.AddOrUpdateCache($"post:{user.id}", user);

            return new GetUserResponse
            {
                User = new Protos.User
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
                findWithFiltersResponse.Users.Add(new Protos.User
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

        //public override Task<LoginUserResponse> LoginUser(LoginUserRequest request, ServerCallContext context)
        //{
        //    //if (user.Username != request.Username)
        //    //{
        //    //    return BadRequest("User not found.");
        //    //}

        //    //if (!VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
        //    //{
        //    //    return BadRequest("Wrong password.");
        //    //}

        //    //string token = CreateToken(user);

        //    //var refreshToken = GenerateRefreshToken();
        //    //SetRefreshToken(refreshToken);

        //    //return Ok(token);

        //}
    }
}
