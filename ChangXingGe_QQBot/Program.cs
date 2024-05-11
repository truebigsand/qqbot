using Sora;
using Sora.Entities;
using Sora.Interfaces;
using Sora.Net.Config;
using Sora.Util;
using System.Text.Json;
using YukariToolBox.LightLog;
using MongoDB;
using MongoDB.Driver;
using Sora.EventArgs.SoraEvent;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using Sora.Entities.Segment.DataModel;
using Sora.Entities.Base;
using System.ComponentModel;
using Sora.Entities.Segment;
using System.Reflection;
using ChangXingGe_QQBot.Models;

namespace ChangXingGe_QQBot;

public class GroupSessionData
{
    public AutoResetEvent Session { get; set; }
    public GroupMessageEventArgs Data { get; set; }
    public GroupSessionData(AutoResetEvent Session, GroupMessageEventArgs Data)
    {
        this.Session = Session;
        this.Data = Data;
    }
}

public static class GlobalData
{
    public static AppSettings settings;
    public static MongoClient mongoClient;
    public static IMongoDatabase db;
    public static IMongoCollection<MessageRecord> messagesCollection;
    public static IMongoCollection<ExceptionRecord> exceptionsCollection;
    public static IMongoCollection<GroupCommandRecord> commandsCollection;
    public static Dictionary<(long, long), GroupSessionData> GroupSession = new();

    public static DateTime StartTime;
    public static string FriendRequestKey = string.Empty;
    public static int SetuLimit;
    public static int PersonalMessageRankLimit;
    public static long LoginUserId;
    public static string LoginUserName = string.Empty;

    public static Dictionary<string, (GroupCommandHandler handler, bool isEnabled)> GroupCommandMap = new(new List<KeyValuePair<string, (GroupCommandHandler handler, bool isEnabled)>>
    {
        new("状态", (GroupCommandHandlers.StatusHandler, true)),
        new("统计", (GroupCommandHandlers.StatisticsHandler, true)),
        new("涩图", (GroupCommandHandlers.SetuHandler, true)),
        new("个人统计", (GroupCommandHandlers.PersonalMessageRanksHandler, true)),
        new("搜图", (GroupCommandHandlers.SearchImageHandler, true)),
        new("测试", (GroupCommandHandlers.TestHandler, true)),
        new("消息结构", (GroupCommandHandlers.MessageStructureHandler, true)),
        new("enable", (GroupCommandHandlers.CommandEnableHandler, true)),
        new("disable", (GroupCommandHandlers.CommandDisableHandler, true)),
        new("指令状态", (GroupCommandHandlers.CommandStatusHandler, true)),
    });
}

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.LogConfiguration
           .EnableConsoleOutput()
           .SetLogLevel(LogLevel.Info);

        //实例化Sora服务
        ISoraService service = SoraServiceFactory.CreateService(new ClientConfig()
        {
            Host = "192.168.1.114",
            Port = 8081,
            SuperUsers = [3244346642L, 194622214L]
        });
        // initialize settings
        GlobalData.settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(File.ReadAllText("appsettings.json"))!;
        GlobalData.StartTime = DateTime.Parse(GlobalData.settings.Store["StartTime"]);
        GlobalData.FriendRequestKey = GlobalData.settings.Store["FriendRequestKey"];
        GlobalData.SetuLimit = int.Parse(GlobalData.settings.Store["SetuLimit"]);
        GlobalData.PersonalMessageRankLimit = int.Parse(GlobalData.settings.Store["PersonalMessageRankLimit"]);
        service.Event.OnClientConnect += async (s, e) =>
        {
            GlobalData.LoginUserId = e.LoginUid;
            (var apiStatus, var userInfo, var qid) = await e.SoraApi.GetUserInfo(e.LoginUid);
            if (apiStatus.RetCode == Sora.Enumeration.ApiType.ApiStatusType.Ok)
            {
                GlobalData.LoginUserName = userInfo.Nick;
            }
            else
            {
                Log.Error("Init", "获取登录用户名失败，提取文本At等功能将不生效");
            }
        };
        // initialize mongodb
        GlobalData.mongoClient = new MongoClient(GlobalData.settings.ConnectionStrings["MongoDB"]);
        GlobalData.db = GlobalData.mongoClient.GetDatabase("qqbot");
        GlobalData.messagesCollection = GlobalData.db.GetCollection<MessageRecord>("messages");
        GlobalData.exceptionsCollection = GlobalData.db.GetCollection<ExceptionRecord>("exceptions");
        GlobalData.commandsCollection = GlobalData.db.GetCollection<GroupCommandRecord>("commands");

        // register event handlers
        service.Event.OnPrivateMessage += async (s, e) =>
        {
            _ = GlobalData.messagesCollection.InsertOneAsync(MessageRecord.FromMessageEventArgs(e));
            //MessageBody messageBody = JsonSerializeToString(e);
            //await e.Reply(messageBody);
        };

        service.Event.OnGroupMessage += async (s, e) =>
        {
            _ = GlobalData.messagesCollection.InsertOneAsync(MessageRecord.FromMessageEventArgs(e));

            if (!GlobalData.GroupSession.ContainsKey((e.SourceGroup.Id, e.Sender.Id)))
            {
                GlobalData.GroupSession.Add((e.SourceGroup.Id, e.Sender.Id), new(new AutoResetEvent(false), null!));
            }
            var session = GlobalData.GroupSession[(e.SourceGroup.Id, e.Sender.Id)];
            session.Session.Set();
            session.Data = e;
            GlobalData.GroupSession[(e.SourceGroup.Id, e.Sender.Id)] = session;

            if (e.IsSelfMessage) return;

            if (e.Message.MessageBody.First().Data is AtSegment seg && seg.Target == e.SoraApi.GetLoginUserId().ToString())
            {
                await GroupAtHandler(e.SoraApi, e, e.Message.MessageBody.Skip(1).Select(x => x.Data));
            }
            else if (e.Message.RawText.Trim().StartsWith($"@{GlobalData.LoginUserName}"))
            {
                await GroupAtHandler(e.SoraApi, e, e.Message.MessageBody.Select(x => x.Data));
            }
        };

        service.Event.OnFriendRequest += async (s, e) =>
        {
            if (e.Comment == GlobalData.FriendRequestKey)
            {
                await e.Accept();
            }
            else
            {
                await e.Reject();
            }
        };

        //启动服务并捕捉错误
        await service.StartService()
                     .RunCatch(e => Log.Error("Sora Service", Log.ErrorLogBuilder(e)));
        await Task.Delay(-1);
    }
    static string JsonSerializeToString<T>(T obj)
    {
        var serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
        });
        var sw = new StringWriter();
        serializer.Serialize(sw, obj);
        return sw.ToString();
    }

    static async Task GroupAtHandler(SoraApi api, GroupMessageEventArgs e, IEnumerable<BaseSegment> segments)
    {
        if (segments.Count() == 0 || segments.First() is not TextSegment textSegment)
        {
            await e.Reply("用法错误");
            return;
        }
        var messageText = e.Message.GetText().Trim();
        if (messageText.StartsWith($"@{GlobalData.LoginUserName}"))
        {
            messageText = messageText.Remove(0, $"@{GlobalData.LoginUserName}".Length);
            messageText.TrimStart();
        }
        var tokens = messageText.Split(' ').Where(x => x != string.Empty).ToArray();

        //await e.Reply(string.Join("\n", tokens));
        var commands = (await GlobalData.commandsCollection.FindAsync(x => x.Keyword == tokens[0])).ToList();
        if (commands.Count == 1)
        {
            var command = commands.Single();
            if (command.IsSuperUserNeeded && !e.IsSuperUser)
            {
                await e.ReplyMessage("仅机器人管理员可使用该指令");
            }
            else if (command.IsEnabled)
            {
                // await Console.Out.WriteLineAsync(command.HandlerName);
                var handler = typeof(GroupCommandHandlers).GetMethod(command.HandlerName);
                if (handler != null)
                {
                    handler.Invoke(null, [api, e, tokens[1..]]);
                }
                else
                {
                    // 理论上这里不应该找不到handler，否则说明MongoDB里的HandlerName填错了
                    await e.Reply($"未定义指令：{tokens[0]}");
                }
            }
            else
            {
                await e.Reply(SoraSegment.Reply(e.Message.MessageId) + $"\"{tokens[0]}\"指令未启用");
            }
        }
        else
        {
            await e.Reply($"未定义指令：{tokens[0]}");
        }
    }
    
}
public static class GroupMessageEventArgsExtensions
{
    public static async Task ReplyMessage(this GroupMessageEventArgs e, params SoraSegment[] segments)
        => await e.Reply(new MessageBody(new[] { SoraSegment.Reply(e.Message.MessageId) }.Concat(segments).ToList()));
}