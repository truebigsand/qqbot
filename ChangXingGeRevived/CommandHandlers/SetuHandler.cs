using ChangXingGeRevived.Extensions;
using ChangXingGeRevived.Models;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event;
using Lagrange.Core.Event.EventArg;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChangXingGeRevived.CommandHandlers;

public class SetuHandler : ICommandHandler
{
    private readonly AppConfig _config;
    private readonly ILogger<SetuHandler> _logger;
    private readonly HttpClient _httpClient;
    public SetuHandler(IOptions<AppConfig> appConfig, ILogger<SetuHandler> logger)
    {
        _config = appConfig.Value;
        _logger = logger;
        _httpClient = new HttpClient();
    }
    private async Task HandleAsync(BotContext bot, EventBase e, string[] args)
    {
        int count = 1;
        if (args.Length == 1)
        {
            if (!int.TryParse(args[0], out int _count) || _count < 1 || _count > _config.BotConfig.SetuLimit)
            {

                await e.ReplyAsync(bot, $"参数必须是[1,{_config.BotConfig.SetuLimit}]的整数");
                return;
            }
            count = _count;
        }
        try
        {
            await Task.WhenAll(
                Enumerable.Repeat(0, count)
                .Select(async _ =>
                    bot.SendMessage(e.CreateMessageBuilder().Image(await _httpClient.GetByteArrayAsync("https://api.anosu.top/img/")).Build())
                )
            ).WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException)
        {
            _logger.LogError("Timeout 10 seconds excceed for sending an setu");
        }
    }

    public async Task HandleFriendAsync(BotContext bot, FriendMessageEvent e, string[] args)
        => await HandleAsync(bot, e, args);

    public async Task HandleGroupAsync(BotContext bot, GroupMessageEvent e, string[] args)
        => await HandleAsync(bot, e, args);
}
