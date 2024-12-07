using ChangXingGeRevived.CommandHandlers;
using ChangXingGeRevived.Databases;
using ChangXingGeRevived.Models;
using ChangXingGeRevived.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace ChangXingGeRevived;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddHostedService<BotService>();
        builder.Services.AddHostedService<GlobalExceptionHandlerService>();

        builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("AppConfig"));

        builder.Services.AddSingleton<MongoClient>(provider =>
            {
                var settings = MongoClientSettings.FromConnectionString(builder.Configuration["ConnectionStrings:MongoDB"]);
                return new MongoClient(settings);
            });
        builder.Services.AddSingleton<CommandListCacheService>();
        builder.Services.AddSingleton<GroupCommandDispatcherService>();
        builder.Services.AddSingleton<GroupSessionService>();
        builder.Services.AddSingleton<BotPersistenceService>();

        builder.Services.AddTransient<StatusHandler>();
        builder.Services.AddTransient<SetuHandler>();
        builder.Services.AddTransient<StatisticsHandler>();
        builder.Services.AddTransient<PersonalMessageRanksHandler>();
        builder.Services.AddTransient<TestHandler>();

        builder.Services.AddDbContext<BotDbContext>((provider, optionsBuilder) =>
        {
            var mongoClient = provider.GetService<MongoClient>()!;
            optionsBuilder.UseMongoDB(mongoClient, "qqbot");
        });

        var host = builder.Build();
        
        host.Run();
    }
}
