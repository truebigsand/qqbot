using ChangXingGeRevived.Databases;
using ChangXingGeRevived.Helpers;
using ChangXingGeRevived.Models;
using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChangXingGeRevived.Services;

public class BotService : IHostedService
{
    private readonly ILogger<BotService> _logger;
    private readonly BotPersistenceService _botPersistenceService;
    private readonly GroupCommandDispatcherService _groupDispatcher;
    private readonly GroupSessionService _groupSessionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly AppConfig _config;
    private BotContext? _bot = null;

    public BotService(ILogger<BotService> logger, BotPersistenceService botPersistenceService, IOptions<AppConfig> appConfig, GroupCommandDispatcherService groupDispatcher, IServiceProvider serviceProvider, GroupSessionService groupSessionService)
    {
        _logger = logger;
        _botPersistenceService = botPersistenceService;
        _config = appConfig.Value;
        _groupDispatcher = groupDispatcher;
        _serviceProvider = serviceProvider;
        _groupSessionService = groupSessionService;
        //var all = db.MessageRecords.ToList();
        //foreach (var record in all)
        //{
        //    record.MessageId += 2000000000;
        //}
        //db.UpdateRange(all);
        //db.SaveChanges();
        //Environment.Exit(0);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BotService started");
        await FetchQrCode();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _bot?.Dispose();
        _logger.LogInformation("BotService stopped");
        return Task.CompletedTask;
    }

    private async Task FetchQrCode()
    {
        var deviceInfo = _botPersistenceService.GetDeviceInfo();
        var keyStore = _botPersistenceService.LoadKeystore();

        var botConfig = new BotConfig
        {
            UseIPv6Network = false,
            GetOptimumServer = true,
            AutoReconnect = true,
            Protocol = Protocols.Linux
        };

        //if (_config.BotConfig.Uin != null && _config.BotConfig.Password != null)
        //{
        //    BotFactory.Create(botConfig, (uint)_config.BotConfig.Uin, _config.BotConfig.Password, out var device);
        //}

        var bot = BotFactory.Create(botConfig, deviceInfo, keyStore);

        bot.Invoker.OnBotLogEvent += (context, @event)
            => _logger.Log(LogLevelHelper.LagrangeLogLevelToMicrosoft(@event.Level), @event.EventMessage);
        
        bot.Invoker.OnBotOnlineEvent += (context, @event) =>
        {
            _logger.LogInformation(@event.ToString());
            _botPersistenceService.SaveKeystore(bot.UpdateKeystore());
        };

        bot.Invoker.OnGroupMessageReceived += (bot, e) =>
        {
            //Console.WriteLine(e.Chain.GroupMemberInfo.Uin);
            _groupSessionService.Update((uint)e.Chain.GroupUin!, e.Chain.GroupMemberInfo!.Uin, e);
            if (e.Chain.GroupMemberInfo?.Uin != bot.BotUin)
            {
                Task.Run(() => { _groupDispatcher.DispatchAsync(bot, e).Wait(); });
            }
            using var db = _serviceProvider.CreateScope().ServiceProvider.GetService<BotDbContext>() ?? throw new Exception("DbContext not found in service provider");
            //var db = BotDbContext
            db.MessageRecords.Add(MessageRecord.FromEvent(e));
            db.SaveChanges();


            //if (e.EventTime.ToLocalTime() != e.Chain.Time.ToLocalTime()){
            //    _logger.LogCritical("e.EventTime != e.Chain.Time, {} <=> {}", e.EventTime.ToLocalTime(), e.Chain.Time.ToLocalTime());
            //}
        };
        if (File.Exists(_config.KeystorePath) && File.Exists(_config.DeviceInfoPath))
        {
            await bot.LoginByPassword();
            _bot = bot;
            return;
        }
        _logger.LogWarning("No exist session, try QRCode");

        var qrCode = await bot.FetchQrCode();
        if (qrCode != null)
        {
            await File.WriteAllBytesAsync("qr.png", qrCode.Value.QrCode);
            QrCodeHelper.PrintToConsole(qrCode.Value.Url, true);
            await bot.LoginByQrCode();
        }
        _bot = bot;
    }
}