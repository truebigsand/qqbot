using ChangXingGeRevived.CommandHandlers;
using ChangXingGeRevived.Databases;
using ChangXingGeRevived.Models;
using ChangXingGeRevived.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ChangXingGeRevived;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddHostedService<BotService>();
        builder.Services.AddHostedService<GlobalExceptionHandlerService>();
        builder.Services.AddHostedService<CommandListUpdateService>();

        builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("AppConfig"));

        builder.Services.AddSingleton<CommandListCacheService>();
        builder.Services.AddSingleton<GroupCommandDispatcherService>();
        builder.Services.AddSingleton<GroupSessionService>();
        builder.Services.AddSingleton<BotPersistenceService>();

        builder.Services.AddTransient<StatusHandler>();
        builder.Services.AddTransient<SetuHandler>();
        builder.Services.AddTransient<StatisticsHandler>();
        builder.Services.AddTransient<PersonalMessageRanksHandler>();
        builder.Services.AddTransient<SigninHandler>();
        builder.Services.AddTransient<TestHandler>();
        builder.Services.AddTransient<DeepSeekHandler>();

        builder.Services.AddDbContext<BotDbContext>((provider, optionsBuilder) =>
        {
            var connectionString = builder.Configuration["ConnectionStrings:MySQL"];
            if (connectionString is null)
            {
                throw new ArgumentNullException("MySQL connection string cannot be null!");
            }
            optionsBuilder.UseMySQL(connectionString);
        });

        var host = builder.Build();
        
        host.Run();
    }
}
