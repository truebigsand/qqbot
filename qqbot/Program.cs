using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Net;
using System.Text;
using System.Diagnostics;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Sessions;
using Mirai.Net.Sessions.Http.Managers;
using System.Reactive.Linq;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Utils.Scaffolds;
using Mirai.Net.Data.Messages;
using System.Reactive;

namespace qqbot
{
    static class GlobalData
    {
        public static DateTime StartTime { get; set; }
        public static string BotQQ { get; set; } = string.Empty;
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
            new("搜图", CommandHandler.SearchImageHandler),
            new("测试", CommandHandler.TestHandler)
        });
        private static Dictionary<(string, string), AutoResetEvent> GroupSession = new();
        private static Dictionary<(string, string), GroupMessageReceiver> GroupSessionData = new();
        // "Server=192.168.0.103; Username=root; Password=feifei070926; Database=qqbot"

        static async Task Main(string[] args)
        {
            GlobalData.StartTime = DateTime.Parse(await GlobalData.db.GetInformationAsync("statistics.start_time"));
            GlobalData.BotQQ = await GlobalData.db.GetInformationAsync("bot.qq");

            Logger.Info("获取统计信息成功!");

            using MiraiBot bot = new MiraiBot()
            {
                Address = await GlobalData.db.GetInformationAsync("mirai.address"),
                VerifyKey = await GlobalData.db.GetInformationAsync("mirai.verify_key"),
                QQ = GlobalData.BotQQ
            };
            await bot.LaunchAsync();
            Logger.Info("登录成功!");
            
            await MessageManager.SendFriendMessageAsync("3244346642", $"机器人上线[{DateTime.Now}]");

            // Insert to database and log
            bot.MessageReceived.OfType<GroupMessageReceiver>().Subscribe(async receiver =>
            {
                string readableString = receiver.MessageChain.ToReadableString();
                Logger.Info($"接收到群聊消息: [{receiver.GroupName}({receiver.GroupId})][{receiver.Sender.Name}({receiver.Sender.Id})]{readableString}");
                await GlobalData.db.InsertGroupMessageAsync(receiver, readableString);
            });

            // Update group session
            bot.MessageReceived.OfType<GroupMessageReceiver>().Subscribe(receiver =>
            {
                if (!GroupSession.ContainsKey((receiver.GroupId, receiver.Sender.Id)))
                {
                    GroupSession.Add((receiver.GroupId, receiver.Sender.Id), new AutoResetEvent(false));
                }
                if (!GroupSessionData.ContainsKey((receiver.GroupId, receiver.Sender.Id)))
                {
                    GroupSessionData.Add((receiver.GroupId, receiver.Sender.Id), new GroupMessageReceiver());
                }
                GroupSessionData[(receiver.GroupId, receiver.Sender.Id)] = receiver;
                GroupSession[(receiver.GroupId, receiver.Sender.Id)].Set();
                Logger.Warning($"group session updated for ({receiver.GroupId}, {receiver.Sender.Id})");  
            });

            // Handle group command
            bot.MessageReceived.OfType<GroupMessageReceiver>().Where(IsAtSelf)
                .Subscribe(async receiver => await HandleGroupCommandAsync(receiver));

            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Q)
                {
                    break;
                }
            }
        }
        // MessageChain structure: 0=>source, 1=>at, 2...=> commands
        private static async Task HandleGroupCommandAsync(GroupMessageReceiver e)
        {
            var messages = e.MessageChain.ToList();
            if(messages == null)
            {
                Logger.Error($"[{e.GroupName}({e.GroupId})][{e.Sender.Name}({e.Sender.Id})]e.MessageChain is null!");
                return;
            }
            if(messages.Count == 2)
            {
                await e.SendMessageAsync("使用方法：@我 指令 [参数]");
                return;
            }
            SourceMessage source = (messages[0] as SourceMessage)!;
            var command = (messages[2] as PlainMessage)?.Text?.Trim().Split(' ').Where(x => x != string.Empty).ToArray()!;
            if (command.Length == 0)
            {
                await e.SendMessageAsync("使用方法：@我 指令 [参数]");
                return;
            }
            string commandIdentity = command[0];
            try
            {
                var handler = GroupCommandHelper.GetHandler(commandIdentity);
                await handler(e, command[1..^0]);
            }
            catch (KeyNotFoundException)
            {
                await e.SendMessageAsync("使用方法：@我 指令 [参数]");
            }
        }
        private static bool IsAtSelf(GroupMessageReceiver e)
        {
            return e.MessageChain.Skip(1).First() is AtMessage message && message.Target == GlobalData.BotQQ;
        }
        public static class CommandHandler
        {
            private static HttpClient httpClient = new HttpClient();
            private static GroupMessageReceiver WaitForNextMessage(GroupMessageReceiver e)
            {
                var are = GroupSession[(e.GroupId, e.Sender.Id)];
                are.Reset();
                are.WaitOne();
                return GroupSessionData[(e.GroupId, e.Sender.Id)];
            }
            public static async Task StatusHandler(GroupMessageReceiver e, string[] args)
            {
                SourceMessage source = (e.MessageChain.First() as SourceMessage)!;
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("当前状态：");
                TimeSpan latency = DateTime.Now - DateTimeConverter.ToDateTime(source.Time);
                sb.AppendLine($"单程延迟：{latency.TotalMilliseconds}ms");
                TimeSpan runningTime = DateTime.Now - GlobalData.StartTime;
                sb.AppendLine($"运行时间：{runningTime.Days}天{runningTime.Hours}小时{runningTime.Minutes}分钟{runningTime.Seconds}秒");
                sb.AppendLine($"接收的消息：{await GlobalData.db.GetMessageCountAsync()}条");
                sb.Append($"处理的消息：{await GlobalData.db.GetHandledMessageCountAsync()}条");

                await e.SendMessageAsync(sb.ToString());
            }
            public static async Task StatisticsHandler(GroupMessageReceiver e, string[] args)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"消息统计(自{GlobalData.StartTime.ToString("yyyy-MM-dd HH:mm:ss")}以来)：");
                sb.AppendLine($"共{await GlobalData.db.GetMessageCountAsync(e.GroupId)}条消息");
                var ranks = GlobalData.db.GetMessageRanksAsync(e.GroupId);
                await foreach (var rank in ranks)
                {
                    sb.AppendLine($"{rank.Name}({rank.Id}): {rank.Count}条");
                }
                await e.SendMessageAsync(sb.ToString().TrimEnd());
            }
            public static async Task SetuHandler(GroupMessageReceiver e, string[] args)
            {
                int count = 1;
                if (args.Length == 1)
                {
                    if (!int.TryParse(args[0], out int _count) || _count < 1 || _count > 5)
                    {
                        await e.SendMessageAsync("参数必须是[1,5]的整数");
                        return;
                    }
                    count = _count;
                }
                for (int i = 0; i < count; i++)
                {
                    _ = e.SendMessageAsync(new ImageMessage() { Url = "https://api.anosu.top/img/" });
                }
            }
            public static async Task PersonalMessageRanksHandler(GroupMessageReceiver e, string[] args)
            {
                int limit = 10;
                if (args.Length == 1)
                {
                    if (!int.TryParse(args[0], out int _limit) || _limit < 1 || _limit > 20)
                    {
                        await e.SendMessageAsync("参数必须是[1,20]的整数");
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
                await e.SendMessageAsync(sb.ToString().TrimEnd());
            }
            public static async Task SearchImageHandler(GroupMessageReceiver e, string[] args)
            {
                await e.SendMessageAsync(new MessageChainBuilder().At(e.Sender).Plain(" 请发送要识别的图片").Build());
                Logger.Debug("wait for image");
                e = WaitForNextMessage(e);
                Logger.Debug("received message");
                var message = e.MessageChain.Skip(1).Take(1).Single();
                string url = string.Empty;
                if (message is ImageMessage imageMessage)
                {
                    url = imageMessage.Url;
                }
                else if (message is FileMessage fileMessage)
                {
                    Logger.Debug("waiting for file link");
                    var file = await FileManager.GetFileAsync(e.GroupId, fileMessage.FileId, true);
                    url = file.DownloadInfo.Url;
                }
                else
                {
                    await e.SendMessageAsync(new MessageChainBuilder().At(e.Sender).Plain(" 你发了甚么啊（恼").Build());
                    return;
                }
                Logger.Debug($"获取到image链接：{url}");
                string api_url = "https://proxy.truebigsand.top/https://saucenao.com/search.php?api_key=14c88397ab04c3f51ebe81ccfde020cfd4fbc5c8&db=999&output_type=2&url=" + url;
                var builder = new MessageChainBuilder();
                try
                {
                    string jsonStr = await httpClient.GetStringAsync(api_url);
                    Logger.Info(jsonStr);
                    var json = JObject.Parse(jsonStr);
                    var header = json["results"]![0]!["header"]!;
                    var data = json["results"]![0]!["data"]!;
                    string similarity = header.Value<string>("similarity")!;
                    string thumbnail = header.Value<string>("thumbnail")!;
                    int index_id = header.Value<int>("index_id");
                    string index_name = header.Value<string>("index_name")!;
                    
                    builder.ImageFromUrl("https://proxy.truebigsand.top/" + thumbnail);
                    builder.Plain($"相似度：{similarity}\n");
                    builder.Plain($"来源：{index_name}\n");
                    if (index_id != 18 && index_id != 38) // not nhentai or ehentai
                    {
                        string link = data["ext_urls"]![0]!.ToObject<string>()!;
                        builder.Plain($"链接：{link}\n");
                    }
                    
                    if (index_id == 5) // Pixiv
                    {
                        string title = data.Value<string>("title")!;
                        int pixiv_id = data.Value<int>("pixiv_id");
                        string member_name = data.Value<string>("member_name")!;
                        int member_id = data.Value<int>("member_id");
                        builder.Plain($"标题：{title}\n");
                        builder.Plain($"作品ID：{pixiv_id}\n");
                        builder.Plain($"画师：{member_name}\n");
                        builder.Plain($"画师ID：{member_id}");
                    }
                    else if (index_id == 18 || index_id == 38) // nhentai or ehentai
                    {
                        string source = data.Value<string>("source")!;
                        string jp_name = data.Value<string>("jp_name")!;
                        builder.Plain($"标题：{source}\n");
                        builder.Plain($"原标题：{jp_name}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                    
                    long error_id = await GlobalData.db.InsertBotErrorAsync(e, ex.Message);
                    await e.SendMessageAsync($"发生内部错误，错误ID: {error_id}");
                    return;
                }
                await MessageManager.SendGroupMessageAsync(e.GroupId, builder.Build());
            }
            public static async Task TestHandler(GroupMessageReceiver e, string[] args)
            {
                await MessageManager.SendGroupMessageAsync(e.GroupId, new PlainMessage("1"));
                Logger.Debug("waiting for session");
                e = WaitForNextMessage(e);
                if ((e.MessageChain.Skip(1).Take(1).Single() as PlainMessage)?.Text == "2")
                {
                    await e.SendMessageAsync("is 2");
                }
                else
                {
                    await e.SendMessageAsync("not 2");
                }
            }
        }
    }
}