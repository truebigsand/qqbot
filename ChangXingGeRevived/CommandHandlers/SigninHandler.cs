using ChangXingGeRevived.Databases;
using ChangXingGeRevived.Models;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Event;
using Lagrange.Core;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ChangXingGeRevived.Extensions;

namespace ChangXingGeRevived.CommandHandlers;

public class SigninHandler : ICommandHandler
{
    private readonly BotDbContext _db;
    public SigninHandler(BotDbContext db)
    {
        _db = db;
    }

    public async Task HandleFriendAsync(BotContext bot, FriendMessageEvent e, string[] args)
    {
        throw new NotImplementedException();
    }

    public async Task HandleGroupAsync(BotContext bot, GroupMessageEvent e, string[] args)
    {
        var todaySignin = _db.SigninRecords.AsNoTracking()
            .Where(x => x.GroupId == e.Chain.GroupUin&& x.SenderId == e.Chain.TargetUin && x.Time.Date == DateTime.Today);
        if (todaySignin.Any())
        {
            await e.ReplyAsync(bot, "你今天签过到啦~试试在其他群签到吧~\n(๑╹◡╹)ﾉ\"\"\"");
        }
        else
        {
            _db.SigninRecords.Add(new() { GroupId = (ulong)e.Chain.GroupUin, SenderId = e.Chain.TargetUin, SenderName = e.Chain.GroupMemberInfo.MemberName, Time = DateTime.Now });
            await _db.SaveChangesAsync();
            var todaySigninCount = await _db.SigninRecords.AsNoTracking().Where(x => x.Time.Date == DateTime.Today).CountAsync();
            var todayThisGroupSigninCount = await _db.SigninRecords.AsNoTracking().Where(x => x.Time.Date == DateTime.Today && x.GroupId == e.Chain.GroupUin).CountAsync();
            var thisGroupSelfSigninDates = await _db.SigninRecords.AsNoTracking().Where(x => x.SenderId == e.Chain.TargetUin && x.GroupId == e.Chain.GroupUin).Select(x => x.Time).ToListAsync();
            thisGroupSelfSigninDates.Reverse(); // 祖宗之Reverse不可变（可能是放一起会调用IEnumerable<>.Reverse
            var thisGroupSelfSigninTotalCount = thisGroupSelfSigninDates.Count();
            var thisGroupSelfConsecutiveSigninCount = 1;
            for (int i = 0; i < thisGroupSelfSigninTotalCount - 1; i++)
            {
                if (thisGroupSelfSigninDates[i].Date - thisGroupSelfSigninDates[i + 1].Date > TimeSpan.FromDays(1))
                {
                    break;
                }
                thisGroupSelfConsecutiveSigninCount++;
            }
            var timeDescriptor = DateTime.Now.Hour switch
            {
                < 10 => "🌅早上",
                >= 10 and < 12 => "☀️上午",
                >= 12 and < 14 => "☀️中午",
                >= 14 and < 18 => "🌆下午",
                >= 17 => "🌙晚上"
            };
            //Console.WriteLine($"{timeDescriptor}好呀！(๑╹◡╹)ﾉ\"\"\"\n" +
            //    $"🍪您是今天第{todaySigninCount}位，也是本群第{todayThisGroupSigninCount}位签到的~\n" +
            //    $"🎉在本群累计签到{thisGroupSelfSigninTotalCount}天，连续签到{thisGroupSelfConsecutiveSigninCount}天啦~");
            await e.ReplyAsync(bot, $"{timeDescriptor}好呀！(๑╹◡╹)ﾉ\"\"\"\n" +
                $"🍪您是今天第{todaySigninCount}位，也是本群第{todayThisGroupSigninCount}位签到的~\n" +
                $"🎉在本群累计签到{thisGroupSelfSigninTotalCount}天，连续签到{thisGroupSelfConsecutiveSigninCount}天啦~");
            //Console.WriteLine("发送成功");
        }
    }
}
