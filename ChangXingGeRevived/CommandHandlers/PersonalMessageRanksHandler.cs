using ChangXingGeRevived.Databases;
using ChangXingGeRevived.Extensions;
using ChangXingGeRevived.Models;
using Lagrange.Core;
using Lagrange.Core.Event.EventArg;
using Microsoft.Extensions.Options;
using System.Text;

namespace ChangXingGeRevived.CommandHandlers;

public class PersonalMessageRanksHandler : ICommandHandler
{
    private readonly AppConfig _config;
    private readonly BotDbContext _db;

    public PersonalMessageRanksHandler(IOptions<AppConfig> appConfig, BotDbContext db)
    {
        _config = appConfig.Value;
        _db = db;
    }

    public async Task HandleFriendAsync(BotContext bot, FriendMessageEvent e, string[] args)
        => await e.ReplyAsync(bot, "此指令仅限群聊使用");

    public async Task HandleGroupAsync(BotContext bot, GroupMessageEvent e, string[] args)
    {
        int limit = 10;
        if (args.Length == 1)
        {
            if (!int.TryParse(args[0], out int _limit) || _limit < 1 || _limit > _config.BotConfig.PersonalMessageRankLimit)
            {
                await e.ReplyAsync(bot, $"参数必须是[1,{_config.BotConfig.PersonalMessageRankLimit}]的整数");
                return;
            }
            limit = _limit;
        }
        var result = _db.MessageRecords
            .AsEnumerable() // Load all into memory(only for mongodb)
            .Where(x => x.GroupId == e.Chain.GroupUin && x.SenderId == e.Chain.GroupMemberInfo?.Uin)
            .GroupBy(x => x.RawText)
            .Select(g => new { Content = g.First().RawText, Name = g.First().SenderName, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(limit);
        var sb = new StringBuilder();
        sb.AppendLine($"{result.First().Name}在本群的个人消息统计(自{_config.BotConfig.StartTime:yyyy-MM-dd HH:mm:ss}以来)：");
        sb.AppendLine($"共{_db.MessageRecords.Count(x => x.GroupId == e.Chain.GroupUin && x.SenderId == e.Chain.GroupMemberInfo!.Uin)}条消息");
        foreach (var rank in result)
        {
            sb.AppendLine($"\"{rank.Content}\"：{rank.Count}条");
        }
        await e.ReplyAsync(bot, sb.ToString().TrimEnd());
    }
}
