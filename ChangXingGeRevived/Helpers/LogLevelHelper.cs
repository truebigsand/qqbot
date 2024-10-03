using Microsoft.Extensions.Logging;

namespace ChangXingGeRevived.Helpers;

public static class LogLevelHelper
{
    public static LogLevel LagrangeLogLevelToMicrosoft(Lagrange.Core.Event.EventArg.LogLevel level) => level switch
    {
        Lagrange.Core.Event.EventArg.LogLevel.Debug => LogLevel.Debug,
        Lagrange.Core.Event.EventArg.LogLevel.Verbose => LogLevel.Debug,
        Lagrange.Core.Event.EventArg.LogLevel.Information => LogLevel.Information,
        Lagrange.Core.Event.EventArg.LogLevel.Warning => LogLevel.Warning,
        Lagrange.Core.Event.EventArg.LogLevel.Exception => LogLevel.Error,
        Lagrange.Core.Event.EventArg.LogLevel.Fatal => LogLevel.Critical,
        _ => throw new NotImplementedException(),
    };
}