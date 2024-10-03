using ChangXingGeRevived.Databases;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChangXingGeRevived.Services;

public class GlobalExceptionHandlerService(ILogger<GlobalExceptionHandlerService> logger, BotDbContext db) : IHostedService
{
    private void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        if (e.IsTerminating)
        {
            logger.LogCritical("Application is terminating, Unhandled exception: {}", exception);
        }
        else
        {
            logger.LogError("Unhandled exception: {}", exception);
        }
        db.ExceptionRecords.Add(new() { Message = exception?.Message ?? "(Null Message)", Source = exception?.Source ?? "Null Source" });
        db.SaveChanges();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        AppDomain.CurrentDomain.UnhandledException += GlobalExceptionHandler;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        AppDomain.CurrentDomain.UnhandledException -= GlobalExceptionHandler;
        return Task.CompletedTask;
    }
}
