using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UsersService.Repository;
using UsersService.Services;
using UsersService.Cache;
using Newtonsoft.Json;
using UsersService.Protos;
using UsersService.Entities;
using UsersService.Tools;
using UsersService.Repository.Auth;
using UsersService.Repository.Users;

namespace UsersService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var configuration = ConfigurationOptions.Parse(Configuration.GetConnectionString("RedisConnection"));
                return ConnectionMultiplexer.Connect(configuration);
            });

            services.AddDbContext<UserAuthDbContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IUsersRepository,UsersRepository>();
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<UsersServiceImplementation>();
            services.AddScoped<AuthServiceImplementation>();
            services.AddScoped<ICacheService,CacheService>();
            services.AddScoped<ISecurityManager, SecurityManager>();
            services.AddScoped<ITokenProvider, TokenProvider>();

            services.AddGrpc();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseRouting();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<UsersServiceImplementation>();
                endpoints.MapGrpcService<AuthServiceImplementation>();
            });
        }
    }
}
