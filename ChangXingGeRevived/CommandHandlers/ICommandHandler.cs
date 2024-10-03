using Lagrange.Core;
using Lagrange.Core.Event.EventArg;

namespace ChangXingGeRevived.CommandHandlers;

public interface ICommandHandler
{
    public Task HandleGroupAsync(BotContext bot, GroupMessageEvent e, string[] args);
    public Task HandleFriendAsync(BotContext bot, FriendMessageEvent e, string[] args);
}
