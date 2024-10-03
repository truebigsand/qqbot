using ChangXingGeRevived.Extensions;
using ChangXingGeRevived.Services;
using Lagrange.Core;
using Lagrange.Core.Event.EventArg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangXingGeRevived.CommandHandlers;

public class TestHandler : ICommandHandler
{
    private readonly GroupSessionService _session;

    public TestHandler(GroupSessionService session)
    {
        _session = session;
    }

    public Task HandleFriendAsync(BotContext bot, FriendMessageEvent e, string[] args)
    {
        throw new NotImplementedException();
    }

    public async Task HandleGroupAsync(BotContext bot, GroupMessageEvent e, string[] args)
    {
        await e.ReplyAsync(bot, "收到测试请求，请发送一条消息");
        e = _session.WaitForNextMessage((uint)e.Chain.GroupUin!, e.Chain.GroupMemberInfo!.Uin);
        await e.ReplyAsync(bot, $"收到第二条消息，内容为：{e.Chain.ToPreviewString()}");
    }
}
