using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;

namespace ChangXingGeRevived.Extensions;

public static class EventBaseExtensions
{
    private static async Task<MessageResult> ReplyAsync(GroupMessageEvent e, BotContext bot, params IMessageEntity[] entities)
    {
        var builder = MessageBuilder.Group((uint)e.Chain.GroupUin!)
            .Forward(e.Chain)
            .Mention(e.Chain.GroupMemberInfo!.Uin)
            .Text(" ");
        foreach (var entity in entities)
        {
            builder.Add(entity);
        }
        return await bot.SendMessage(builder.Build());
    }
    private static async Task<MessageResult> ReplyAsync(FriendMessageEvent e, BotContext bot, params IMessageEntity[] entities)
    {
        var builder = MessageBuilder.Group((uint)e.Chain.GroupUin!);
        foreach (var entity in entities)
        {
            builder.Add(entity);
        }
        return await bot.SendMessage(builder.Build());
    }
    public static async Task<MessageResult> ReplyAsync(this EventBase e, BotContext bot, params IMessageEntity[] entities) => e switch
    {
        GroupMessageEvent groupEvent => await ReplyAsync(groupEvent, bot, entities),
        FriendMessageEvent friendEvent => await ReplyAsync(friendEvent, bot, entities),
        TempMessageEvent tempEvent => throw new NotImplementedException(),
        _ => throw new InvalidOperationException("Unknown message event type")
    };
    public static async Task<MessageResult> ReplyAsync(this EventBase e, BotContext bot, string text)
        => await ReplyAsync(e, bot, new TextEntity(text));

    public static MessageBuilder CreateMessageBuilder(this EventBase e)
        => e switch
        {
            FriendMessageEvent friendEvent => MessageBuilder.Friend(friendEvent.Chain.FriendUin),
            GroupMessageEvent groupEvent => MessageBuilder.Group((uint)groupEvent.Chain.GroupUin!),
            _ => throw new NotImplementedException()
        };
}
