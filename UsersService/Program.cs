namespace UsersService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = CreateHostBuilder(args);
            var app = builder.Build();
            app.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.ListenAnyIP(49001, listenOptions =>
                        {
                            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
                        });
                    });
                    webBuilder.UseStartup<Startup>(); // ”казываетс€ Startup-класс
                });
    }
}
