using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UsersService.Repository;
using UsersService.Services;
using UsersService.Cache;
using Newtonsoft.Json;
using UsersService.Protos;
using UsersService.Entities;

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

            services.AddScoped<UsersRepository>();
            services.AddScoped<AuthRepository>();
            services.AddScoped<UsersServiceImplementation>();
            services.AddScoped<AuthServiceImplementation>();
            services.AddScoped<SecurityService>();
            services.AddScoped<CacheService>();

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

            //app.Use(async (context, next) =>
            //{
            //    // Получаем сервис провайдер из контекста запроса
            //    var serviceProvider = context.RequestServices;

            //    // Получаем scoped-сервис PostsRepository
            //    var usersRepository = serviceProvider.GetRequiredService<UsersRepository>();

            //    var authRepository = serviceProvider.GetRequiredService<AuthRepository>();

            //    // Получаем подключение к Redis
            //    var connectionMultiplexer = serviceProvider.GetRequiredService<IConnectionMultiplexer>();

            //    // Инициализируем кэш асинхронно
            //    await InitializeCacheAsync(usersRepository, connectionMultiplexer);

            //    await InitializeCacheAsync(authRepository, connectionMultiplexer);

            //    // Передаем управление следующему middleware
            //    await next();
            //});

            app.Use(async (context, next) =>
            {
                // Получаем сервис провайдер из контекста запроса
                var serviceProvider = context.RequestServices;

                // Получаем scoped-сервис UsersRepository
                var usersRepository = serviceProvider.GetRequiredService<UsersRepository>();

                // Получаем scoped-сервис AuthRepository
                var authRepository = serviceProvider.GetRequiredService<AuthRepository>();

                // Получаем подключение к Redis
                var connectionMultiplexer = serviceProvider.GetRequiredService<IConnectionMultiplexer>();

                // Инициализируем кэш для UsersRepository
                await InitializeCacheAsync(usersRepository, authRepository, connectionMultiplexer);

                // Передаем управление следующему middleware
                await next();
            });

            app.UseRouting();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<UsersServiceImplementation>();
                endpoints.MapGrpcService<AuthServiceImplementation>();
            });
        }

        private async Task InitializeCacheAsync(UsersRepository usersRepository, AuthRepository authRepository, IConnectionMultiplexer connectionMultiplexer)
        {
            // Получаем данные из репозитория
            var users = usersRepository.GetAll();
            var authInfos = authRepository.GetAll();

            // Подключение к Redis
            var database = connectionMultiplexer.GetDatabase();

            // Проходим по всем записям и добавляем/обновляем данные в кэше
            foreach (var user in users)
            {
                var cacheKey = $"user:{user.id}";

                // Преобразовываем объект в JSON (или любой другой формат)
                var serializedPost = JsonConvert.SerializeObject(user);

                // Записываем данные в Redis
                await database.StringSetAsync(cacheKey, serializedPost);
            }

            foreach(var authInfo in authInfos)
            {
                var cacheKey = $"user:{authInfo.id}";

                // Преобразовываем объект в JSON (или любой другой формат)
                var serializedPost = JsonConvert.SerializeObject(authInfo);

                // Записываем данные в Redis
                await database.StringSetAsync(cacheKey, serializedPost);
            }
        }
    }
}
