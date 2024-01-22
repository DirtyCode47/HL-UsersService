
using Grpc.Core;
using Npgsql;
using System.Data.Common;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using static Grpc.Core.Metadata;
using System;

using UsersService.Repository;
using UsersService.Protos;
using UsersService.Entities;
using User = UsersService.Entities.User;
using Google.Protobuf.Collections;
using System.Net;

namespace UsersService.Services
{
    public class UsersServiceImplementation : Protos.UsersService.UsersServiceBase
    {
        UsersRepository usersRepository;
        public UsersServiceImplementation(UsersRepository usersRepository)
        {
            this.usersRepository = usersRepository;
        }

        public Task<CreateUserResponse> Create(CreateUserRequest request, ServerCallContext context)
        {
            Guid user_id = Guid.Parse(request.User.Id);

            User user = new User()
            {
                id = user_id,
                role = request.User.Role,
                post_code = request.User.PostCode,
                first_name = request.User.FirstName,
                middle_name = request.User.MiddleName,
                last_name = request.User.LastName,
                phone = request.User.Phone,
            };

            if (usersRepository.GetUser(user_id) != null)
                throw new RpcException(new Status(StatusCode.AlreadyExists, "This record already exist in Db"));

            User added_user = usersRepository.CreateUser(user);
            usersRepository.Complete();

            return Task.FromResult(
                new CreateUserResponse 
                { 
                    User = request.User 
                });
        }

        public Task<DeleteUserResponse> Delete(DeleteUserRequest request, ServerCallContext context)
        {
            Guid user_id = Guid.Parse(request.Id);

            User user = usersRepository.GetUser(user_id);
            if (user == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Can't find record in Db with this id"));

            usersRepository.DeleteUser(user_id);
            usersRepository.Complete();

            return Task.FromResult(
                new DeleteUserResponse
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
                });
        }

        public Task<UpdateUserResponse> Update(UpdateUserRequest request, ServerCallContext context)
        {
            User user = new User()
            {
                id = Guid.Parse(request.User.Id),
                role = request.User.Role,
                post_code = request.User.PostCode,
                first_name = request.User.FirstName,
                middle_name = request.User.MiddleName,
                last_name = request.User.LastName,
                phone = request.User.Phone,
            };

            var entry = usersRepository.UpdateUser(user);
            if (entry == null) 
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Can't find record in Db with this id"));

            usersRepository.Complete();

            return Task.FromResult(
                new UpdateUserResponse
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
                });
        }


        public Task<GetUserResponse> Get(GetUserRequest request, ServerCallContext context)
        {
            Guid guid = Guid.Parse(request.Id);

            User user = usersRepository.GetUser(guid);
            if (user == null) throw new RpcException(new Status(StatusCode.InvalidArgument, "Can't find record in Db with this id"));

            return Task.FromResult(
                new GetUserResponse
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
                });
        }

        public Task<FindUsersWithFiltersResponse> FindWithFilters(FindUsersWithFiltersRequest request, ServerCallContext context)
        {
            var users = usersRepository.FindUsersWithFilters(request.Name,request.Role,request.PostCode);
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
    }
}
