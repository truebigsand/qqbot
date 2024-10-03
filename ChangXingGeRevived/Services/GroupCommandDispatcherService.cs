using ChangXingGeRevived.CommandHandlers;
using ChangXingGeRevived.Extensions;
using ChangXingGeRevived.Models;
using Lagrange.Core;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message.Entity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChangXingGeRevived.Services;

public class GroupCommandDispatcherService
{
    private readonly ILogger<GroupCommandDispatcherService> _logger;
    private readonly CommandListCacheService _commandList;
    private readonly IServiceProvider _serviceProvider;
    private readonly AppConfig _config;
    public GroupCommandDispatcherService(ILogger<GroupCommandDispatcherService> logger, CommandListCacheService commandList, IServiceProvider serviceProvider, IOptions<AppConfig> appConfig)
    {
        _logger = logger;
        _commandList = commandList;
        _serviceProvider = serviceProvider;
        _config = appConfig.Value;
    }

    public async Task DispatchAsync(BotContext bot, GroupMessageEvent e)
    {
        var previewString = e.Chain.ToPreviewString();
        _logger.LogInformation("Preview String: {}", previewString);
        //_logger.LogInformation("Preview Text: {}", e.Chain.ToPreviewText());
        // is at self
        string[] tokens;
        if (e.Chain.First() is MentionEntity entity && entity.Uin == bot.BotUin)
        {
            var segments = e.Chain.Skip(1);
            if (segments.Any() && segments.First() is TextEntity textEntity)
            {
                tokens = textEntity.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
            else
            {
                await e.ReplyAsync(bot, "用法错误");
                return;
            }
        }
        else if (previewString.Contains($"@{bot.BotName}"))
        {
            // logger.LogInformation("Received At Self");
            var mentionString = $"@{bot.BotName}";
            var promptString = previewString.Substring(previewString.IndexOf(mentionString) + mentionString.Length);
            tokens = promptString.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        else
        {
            return;
        }

        if (!_commandList.TryGetCommand(tokens[0], out var command))
        {
            await e.ReplyAsync(bot, $"未定义指令：{tokens[0]}");
            return;
        }
        if (command.IsSuperUserNeeded && !_config.BotConfig.SuperUsers.Contains(e.Chain.GroupMemberInfo!.Uin))
        {
            await e.ReplyAsync(bot, "仅机器人管理员可使用该指令");
            return;
        }
        if (!command.IsEnabled)
        {
            await e.ReplyAsync(bot, $"\"{tokens[0]}\"指令未启用");
            return;
        }
        var type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => x.Name == command.HandlerName).FirstOrDefault()
            ?? throw new Exception($"Command handler not found: {command.HandlerName}");
        var handler = _serviceProvider.GetService(type) as ICommandHandler
            ?? throw new Exception($"Command handler not found in service provider: {command.HandlerName}");
        await handler.HandleGroupAsync(bot, e, tokens[1..]);
    }
}
