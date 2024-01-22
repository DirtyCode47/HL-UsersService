using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

using UsersService.Repository;
using UsersService.Services;
using UsersService.Cache;
using Newtonsoft.Json;

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

            services.AddDbContext<UsersDbContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<UsersRepository>();
            services.AddScoped<UsersServiceImplementation>();
            services.AddScoped<CacheService>();
            services.AddScoped<AuthService>();

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

            app.Use(async (context, next) =>
            {
                // Получаем сервис провайдер из контекста запроса
                var serviceProvider = context.RequestServices;

                // Получаем scoped-сервис PostsRepository
                var usersRepository = serviceProvider.GetRequiredService<UsersRepository>();

                // Получаем подключение к Redis
                var connectionMultiplexer = serviceProvider.GetRequiredService<IConnectionMultiplexer>();

                // Инициализируем кэш асинхронно
                await InitializeCacheAsync(usersRepository, connectionMultiplexer);

                // Передаем управление следующему middleware
                await next();
            });

            app.UseRouting();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<UsersServiceImplementation>();
            });
        }

        private async Task InitializeCacheAsync(UsersRepository usersRepository, IConnectionMultiplexer connectionMultiplexer)
        {
            // Получаем данные из репозитория
            var users = usersRepository.GetAllUsers();

            // Подключение к Redis
            var database = connectionMultiplexer.GetDatabase();

            // Проходим по всем записям и добавляем/обновляем данные в кэше
            foreach (var user in users)
            {
                var cacheKey = $"post:{user.id}";

                // Преобразовываем объект в JSON (или любой другой формат)
                var serializedPost = JsonConvert.SerializeObject(user);

                // Записываем данные в Redis
                await database.StringSetAsync(cacheKey, serializedPost);
            }
        }
    }
}
