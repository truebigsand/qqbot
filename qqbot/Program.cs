using Newtonsoft.Json.Linq;
using System.Text.Json;
using Chaldene.Sessions;
using Chaldene.Data.Messages.Concretes;
using Chaldene.Data.Messages;
using System.Net;
using System.Text;
using Chaldene.Data.Messages.Receivers;
using Chaldene.Data.Shared;

namespace qqbot
{
    static class GlobalData
    {
        public static DateTime StartTime { get; set; }
        public static DbHelper db = new DbHelper(builder =>
        {
            builder.Server = "192.168.0.103";
            builder.UserID = "root";
            builder.Password = "feifei070926";
            builder.Database = "qqbot";

            // seconds for tcp connection to keep alive (0 for not keep alive)
            // builder.Keepalive = ;
        });
    }
    class Program
    {
        private static GroupCommandHelper GroupCommandHelper = new GroupCommandHelper(new List<KeyValuePair<string, GroupCommandHandler>>
        {
            new("状态", CommandHandler.StatusHandler),
            new("统计", CommandHandler.StatisticsHandler),
            new("涩图", CommandHandler.SetuHandler),
            new("个人统计", CommandHandler.PersonalMessageRanksHandler),
            new("搜图", CommandHandler.SearchImageHandler)
        });
        private static Dictionary<(string, string), AutoResetEvent> GroupSession = new();
        private static Dictionary<(string, string), GroupMessageReceiver> GroupSessionData = new();
        // "Server=192.168.0.103; Username=root; Password=feifei070926; Database=qqbot"
        private static DbHelper db = new DbHelper(builder =>
        {
            builder.Server = "192.168.0.103";
            builder.UserID = "root";
            builder.Password = "feifei070926";
            builder.Database = "qqbot";

            // seconds for tcp connection to keep alive (0 for not keep alive)
            // builder.Keepalive = ;
        });
        static async Task Main(string[] args)
        {
            GlobalData.StartTime = DateTime.Parse(await db.GetInformationAsync("statistics.start_time"));
            Logger.Info("获取统计信息成功!");

            using MiraiBot bot = new MiraiBot("127.0.0.1:8080", "ab0096f1-f9b9-4df3-ab12-5eeef48bf48f", 2628754644);
            await bot.LaunchAsync();
            Logger.Info("登录成功!");
            
            await bot.SendFriendMessageAsync(3244346642L, $"机器人上线[{DateTime.Now}]");
            
            
            bot.GroupMessageReceived += async (sender, e) =>
            {
                string readableString = e.MessageChain.ToReadableString();
                Logger.Info($"接收到群聊消息: [{e.GroupName}({e.GroupId})][{e.Sender.Name}({e.Sender.Id})]{readableString}");

                if (!GroupSession.ContainsKey((e.GroupId, e.Sender.Id)))
                {
                    GroupSession.Add((e.GroupId, e.Sender.Id), new AutoResetEvent(false));
                }
                if (!GroupSessionData.ContainsKey((e.GroupId, e.Sender.Id)))
                {
                    GroupSessionData.Add((e.GroupId, e.Sender.Id), new GroupMessageReceiver());
                }
                GroupSessionData[(e.GroupId, e.Sender.Id)] = e;
                GroupSession[(e.GroupId, e.Sender.Id)].Set();
                Logger.Warning($"group session updated for ({e.GroupId}, {e.Sender.Id})");

                db.InsertGroupMessageAsync(e, readableString);
                if (IsAtSelf(sender, e))
                {
                    await HandleGroupCommandAsync(sender, e);
                }
            };

            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Q)
                {
                    break;
                }
            }
        }
        // MessageChain structure: 0=>source, 1=>at, 2...=> commands
        private static async Task HandleGroupCommandAsync(MiraiBot sender, GroupMessageReceiver e)
        {
            var messages = e.MessageChain.ToList();
            if(messages == null)
            {
                Logger.Error($"[{e.GroupName}({e.GroupId})][{e.Sender.Name}({e.Sender.Id})]e.MessageChain is null!");
                return;
            }
            if(messages.Count == 2)
            {
                sender.SendGroupMessageAsync(e.GroupId, new PlainMessage("使用方法：@我 指令 [参数]"));
                return;
            }
            SourceMessage source = messages[0] as SourceMessage;
            var command = (messages[2] as PlainMessage)?.Text?.Trim().Split(' ').Where(x => x != string.Empty).ToArray();
            if (command.Length == 0)
            {
                sender.SendGroupMessageAsync(e.GroupId, new PlainMessage("使用方法：@我 指令 [参数]"));
                return;
            }
            string commandIdentity = command[0];
            try
            {
                var handler = GroupCommandHelper.GetHandler(commandIdentity);
                handler(sender, e, command[1..^0]);
            }
            catch (KeyNotFoundException)
            {
                sender.SendGroupMessageAsync(e.GroupId, new PlainMessage("使用方法：@我 指令 [参数]"));
            }
        }
        private static bool IsAtSelf(MiraiBot sender, GroupMessageReceiver e)
        {
            return e.MessageChain.Skip(1).First() is AtMessage message && message.Target == sender.QQ;
        }
        public static class CommandHandler
        {
            private static GroupMessageReceiver WaitForNextMessage(GroupMessageReceiver e)
            {
                var are = GroupSession[(e.GroupId, e.Sender.Id)];
                are.Reset();
                are.WaitOne();
                return GroupSessionData[(e.GroupId, e.Sender.Id)];
            }
            public static async Task StatusHandler(MiraiBot bot, GroupMessageReceiver e, string[] args)
            {
                SourceMessage source = e.MessageChain.First() as SourceMessage;
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("当前状态：");
                TimeSpan latency = DateTime.Now - DateTimeConverter.ToDateTime(source.Time);
                sb.AppendLine($"单程延迟：{latency.TotalMilliseconds}ms");
                TimeSpan runningTime = DateTime.Now - GlobalData.StartTime;
                sb.AppendLine($"运行时间：{runningTime.Days}天{runningTime.Hours}小时{runningTime.Minutes}分钟{runningTime.Seconds}秒");
                sb.AppendLine($"接收的消息：{await GlobalData.db.GetMessageCountAsync()}条");
                sb.Append($"处理的消息：{await GlobalData.db.GetHandledMessageCountAsync()}条");

                bot.SendGroupMessageAsync(e.GroupId, new PlainMessage(sb.ToString()));
            }
            public static async Task StatisticsHandler(MiraiBot bot, GroupMessageReceiver e, string[] args)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"消息统计(自{GlobalData.StartTime.ToString("yyyy-MM-dd HH:mm:ss")}以来)：");
                sb.AppendLine($"共{await GlobalData.db.GetMessageCountAsync(e.GroupId)}条消息");
                var ranks = GlobalData.db.GetMessageRanksAsync(e.GroupId);
                await foreach (var rank in ranks)
                {
                    sb.AppendLine($"{rank.Name}({rank.Id}): {rank.Count}条");
                }
                bot.SendGroupMessageAsync(e.GroupId, new PlainMessage(sb.ToString().TrimEnd()));
            }
            public static async Task SetuHandler(MiraiBot bot, GroupMessageReceiver e, string[] args)
            {
                int count = 1;
                if (args.Length == 1)
                {
                    if (!int.TryParse(args[0], out int _count) || _count < 1 || _count > 5)
                    {
                        bot.SendGroupMessageAsync(e.GroupId, new PlainMessage("参数必须是[1,5]的整数"));
                        return;
                    }
                    count = _count;
                }
                for (int i = 0; i < count; i++)
                {
                    bot.SendGroupMessageAsync(e.GroupId, new ImageMessage() { Url = "https://api.anosu.top/img/" });
                }
            }
            public static async Task PersonalMessageRanksHandler(MiraiBot bot, GroupMessageReceiver e, string[] args)
            {
                int limit = 10;
                if (args.Length == 1)
                {
                    if (!int.TryParse(args[0], out int _limit) || _limit < 1 || _limit > 20)
                    {
                        bot.SendGroupMessageAsync(e.GroupId, new PlainMessage("参数必须是[1,20]的整数"));
                        return;
                    }
                    limit = _limit;
                }
                var sb = new StringBuilder();
                sb.AppendLine($"个人消息统计(自{GlobalData.StartTime.ToString("yyyy-MM-dd HH:mm:ss")}以来)：");
                var ranks = GlobalData.db.GetPersonalMessageRanksAsync(e.GroupId, e.Sender.Id, limit);
                await foreach (var rank in ranks)
                {
                    sb.AppendLine($"{rank.Content}: {rank.Count}条");
                }
                bot.SendGroupMessageAsync(e.GroupId, new PlainMessage(sb.ToString().TrimEnd()));
            }
            public static async Task SearchImageHandler(MiraiBot bot, GroupMessageReceiver e, string[] args)
            {
                e = WaitForNextMessage(e);
                if (e.MessageChain.Skip(1).Take(1).Single() is not ImageMessage)
                {
                    bot.SendGroupMessageAsync(e.GroupId, new AtMessage(e.Sender), " 请发送一张图片！");
                    return;
                }
                
            }
            public static async Task TestHandler(MiraiBot bot, GroupMessageReceiver e, string[] args)
            {
                await bot.SendGroupMessageAsync(e.GroupId, new PlainMessage("1"));
                Logger.Warning("waiting for session");
                e = WaitForNextMessage(e);
                if ((e.MessageChain.Skip(1).Take(1).Single() as PlainMessage).Text == "2")
                {
                    bot.SendGroupMessageAsync(e.GroupId, "is 2");
                }
                else
                {
                    bot.SendGroupMessageAsync(e.GroupId, "not 2");
                }
            }
        }
    }
    
}