using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Websocket.Client.Logging;

namespace qqbot
{
    public enum LogLevel
    {
        Debug, Info, Warning, Error, Fatal
    }
    static public class Logger
    {
        public static TextWriter Out = Console.Out;
        private static void WriteColored(string content, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Out.Write(content);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void Log(LogLevel level, string content)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            sb.Append(DateTime.Now);
            sb.Append(']');
            sb.Append('[');
            sb.Append(Enum.GetName(level)?.ToUpper());
            sb.Append(']');
            sb.Append(content);
            sb.Append('\n');
            ConsoleColor color = level switch
            {
                LogLevel.Debug => ConsoleColor.Blue,
                LogLevel.Info => ConsoleColor.Gray,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Fatal => ConsoleColor.DarkRed,
                _ => throw new ArgumentException("undefined level!", nameof(level))
            };
            WriteColored(sb.ToString(), color);
        }
        public static void Debug(string content) => Log(LogLevel.Debug, content);
        public static void Info(string content) => Log(LogLevel.Info, content);
        public static void Warning(string content) => Log(LogLevel.Warning, content);
        public static void Error(string content) => Log(LogLevel.Error, content);
        public static void Fatal(string content) => Log(LogLevel.Fatal, content);
    }
}
