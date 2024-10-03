using ChangXingGeRevived.Databases;
using ChangXingGeRevived.Extensions;
using ChangXingGeRevived.Models;
using Lagrange.Core;
using Lagrange.Core.Event;
using Lagrange.Core.Event.EventArg;
using Microsoft.Extensions.Options;
using System.Text;
using System.Collections.Generic;
using MongoDB.Driver.Linq;

namespace ChangXingGeRevived.CommandHandlers;

public class StatisticsHandler : ICommandHandler
{
    private readonly BotDbContext _db;
    private readonly AppConfig _config;

    public StatisticsHandler(BotDbContext db, IOptions<AppConfig> appConfig)
    {
        _db = db;
        _config = appConfig.Value;
    }

    public async Task HandleFriendAsync(BotContext bot, FriendMessageEvent e, string[] args)
        => await e.ReplyAsync(bot, "此指令仅限群聊使用");

    public async Task HandleGroupAsync(BotContext bot, GroupMessageEvent e, string[] args)
    {
        var result = _db.MessageRecords
            .AsEnumerable() // Load all into memory(only for mongodb)
            .Select(x => new { x.GroupId, x.SenderName, x.SenderId })
            .Where(x => x.GroupId == e.Chain.GroupUin)
            .GroupBy(x => x.SenderId)
            .Select(g => new { Name = g.First().SenderName, Id = g.First().SenderId, Count = g.Count() })
            .OrderByDescending(x => x.Count);
        var sb = new StringBuilder();
        sb.AppendLine($"消息统计(自{_config.BotConfig.StartTime:yyyy-MM-dd HH:mm:ss}以来)：");
        sb.AppendLine($"共{_db.MessageRecords.Count(x => x.GroupId == e.Chain.GroupUin)}条消息");
        foreach (var rank in result)
        {
            sb.AppendLine($"{rank.Name}({rank.Id})：{rank.Count}条");
        }
        await e.ReplyAsync(bot, sb.ToString().TrimEnd());
    }
}
