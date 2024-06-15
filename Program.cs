using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            // 記錄未處理的異常到文件
            File.WriteAllText("service_error.log", ex.ToString());
            throw;
        }        
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;
                Console.WriteLine(env.EnvironmentName);
                config.SetBasePath(env.ContentRootPath)
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel(serverOptions =>
                {
                    serverOptions.ListenAnyIP(5050); // 在所有網絡接口上監聽端口 5000
                    // serverOptions.ListenAnyIP(5001, listenOptions =>
                    // {
                    //     listenOptions.UseHttps(); // 配置 HTTPS
                    // });
                });
                webBuilder.UseStartup<Startup>();
            });
}
