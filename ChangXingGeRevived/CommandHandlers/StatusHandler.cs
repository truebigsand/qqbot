using ChangXingGeRevived.Databases;
using ChangXingGeRevived.Extensions;
using ChangXingGeRevived.Models;
using Lagrange.Core;
using Lagrange.Core.Event;
using Lagrange.Core.Event.EventArg;
using Microsoft.Extensions.Options;
using System.Text;

namespace ChangXingGeRevived.CommandHandlers;

public class StatusHandler : ICommandHandler
{
    private readonly AppConfig _config;
    private readonly BotDbContext _db;
    public StatusHandler(IOptions<AppConfig> appConfig, BotDbContext db)
    {
        _config = appConfig.Value;
        _db = db;
    }
    public string Handle(BotContext bot, EventBase e)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("当前状态：");
        TimeSpan latency = DateTime.Now - e.EventTime;
        sb.AppendLine($"单程延迟：{latency.TotalMilliseconds}ms");
        TimeSpan runningTime = DateTime.Now - _config.BotConfig.StartTime;
        sb.AppendLine($"运行时间：{runningTime.Days}天{runningTime.Hours}小时{runningTime.Minutes}分钟{runningTime.Seconds}秒");
        sb.AppendLine($"接收的消息：{_db.MessageRecords.Count()}条");
        sb.Append($"处理的消息：{_db.MessageRecords.Count(x => x.SenderId == bot.BotUin)}条");
        return sb.ToString();
    }

    public async Task HandleFriendAsync(BotContext bot, FriendMessageEvent e, string[] args)
    {
        await e.ReplyAsync(bot, Handle(bot, e));
    }

    public async Task HandleGroupAsync(BotContext bot, GroupMessageEvent e, string[] args)
    {
        await e.ReplyAsync(bot, Handle(bot, e));
    }
}
