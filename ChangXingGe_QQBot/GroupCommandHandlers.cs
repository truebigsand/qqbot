using Sora.Entities.Base;
using Sora.EventArgs.SoraEvent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using Sora.Entities;
using Sora.Entities.Segment.DataModel;
using Sora.Entities.Segment;
using MongoDB.Driver.Linq;
using ChatGPT.Net;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using YukariToolBox.LightLog;
using System.Web;
using Sora.Serializer;
using ChatGPT.Net.DTO.ChatGPTUnofficial;
using System.Diagnostics.Metrics;
using System.Net;
using ChangXingGe_QQBot.Models;

namespace ChangXingGe_QQBot
{
    public delegate Task GroupCommandHandler(SoraApi api, GroupMessageEventArgs e, string[] args);
    public static class GroupCommandHandlers
    {
        private static HttpClient httpClient = new HttpClient();
        private static GroupMessageEventArgs WaitForNextMessage(GroupMessageEventArgs e)
        {
            var are = GlobalData.GroupSession[(e.SourceGroup.Id, e.Sender.Id)].Session;
            are.Reset();
            are.WaitOne();
            return GlobalData.GroupSession[(e.SourceGroup.Id, e.Sender.Id)].Data;
        }
        public static async Task StatusHandler(SoraApi api, GroupMessageEventArgs e, string[] args)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("当前状态：");
            TimeSpan latency = DateTime.Now - e.Time;
            sb.AppendLine($"单程延迟：{latency.TotalMilliseconds}ms");
            TimeSpan runningTime = DateTime.Now - GlobalData.StartTime;
            sb.AppendLine($"运行时间：{runningTime.Days}天{runningTime.Hours}小时{runningTime.Minutes}分钟{runningTime.Seconds}秒");
            sb.AppendLine($"接收的消息：{GlobalData.messagesCollection.CountDocuments(x => true)}条");
            sb.Append($"处理的消息：{GlobalData.messagesCollection.CountDocuments(x => x.SenderId == api.GetLoginUserId())}条");
            await e.Reply(sb.ToString());
        }
        public static async Task SetuHandler(SoraApi api, GroupMessageEventArgs e, string[] args)
        {
            int count = 1;
            if (args.Length == 1)
            {
                if (!int.TryParse(args[0], out int _count) || _count < 1 || _count > GlobalData.SetuLimit)
                {
                    await e.Reply($"参数必须是[1,{GlobalData.SetuLimit}]的整数");
                    return;
                }
                count = _count;
            }
            //string result = await httpClient.GetStringAsync($"https://api.lolicon.app/setu/v2?num={count}&r18=0");
            //var json = JObject.Parse(result);
            //foreach (JObject image in json["data"]!)
            //{
            //    string url = image["urls"]!.Value<string>("original")!;
            //    _ = e.SendMessageAsync(new ImageMessage() { Url = url });
            //}
            for (int i = 0; i < count; i++)
            {
                _ = e.Reply([SoraSegment.Image("https://api.anosu.top/img/")]);
            }
        }
        public static async Task StatisticsHandler(SoraApi api, GroupMessageEventArgs e, string[] args)
        {
            var q = GlobalData.messagesCollection.AsQueryable();
            var result = q.Where(x => x.GroupId == e.SourceGroup.Id)
                .GroupBy(x => x.SenderId)
                .Select(g => new { Name = g.First().SenderName, Id = g.First().SenderId, Count = g.Count() })
                .OrderByDescending(x => x.Count);
            var sb = new StringBuilder();
            sb.AppendLine($"消息统计(自{GlobalData.StartTime.ToString("yyyy-MM-dd HH:mm:ss")}以来)：");
            sb.AppendLine($"共{GlobalData.messagesCollection.CountDocuments(x => x.GroupId == e.SourceGroup.Id)}条消息");
            foreach (var rank in result)
            {
                sb.AppendLine($"{rank.Name}({rank.Id})：{rank.Count}条");
            }
            await e.Reply(sb.ToString().TrimEnd());
        }
        public static async Task PersonalMessageRanksHandler(SoraApi api, GroupMessageEventArgs e, string[] args)
        {
            int limit = 10;
            if (args.Length == 1)
            {
                if (!int.TryParse(args[0], out int _limit) || _limit < 1 || _limit > GlobalData.PersonalMessageRankLimit)
                {
                    await e.Reply($"参数必须是[1,{GlobalData.PersonalMessageRankLimit}]的整数");
                    return;
                }
                limit = _limit;
            }
            var q = GlobalData.messagesCollection.AsQueryable();
            var result = q.Where(x => x.GroupId == e.SourceGroup.Id && x.SenderId == e.Sender.Id)
                .GroupBy(x => x.RawText)
                .Select(g => new { Content = g.First().RawText, Name = g.First().SenderName, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(limit);
            var sb = new StringBuilder();
            sb.AppendLine($"{result.First().Name}在本群的个人消息统计(自{GlobalData.StartTime.ToString("yyyy-MM-dd HH:mm:ss")}以来)：");
            sb.AppendLine($"共{GlobalData.messagesCollection.CountDocuments(x => x.GroupId == e.SourceGroup.Id && x.SenderId == e.Sender.Id)}条消息");
            foreach (var rank in result)
            {
                sb.AppendLine($"\"{rank.Content}\"：{rank.Count}条");
            }
            await e.Reply(sb.ToString().TrimEnd());
        }
        public static async Task SearchImageHandler(SoraApi api, GroupMessageEventArgs e, string[] args)
        {
            _ = e.Reply("请发送要识别的图片");
            e = WaitForNextMessage(e);
            var urls = GetAllBetween(e.Message.RawText, "[CQ:image,file=", "]");
            if (!urls.Any())
            {
                await e.Reply(SoraSegment.Reply(e.Message.MessageId) + "你发了甚么啊（恼");
                return;
            }
            string api_url = "https://proxy.truebigsand.top/https://saucenao.com/search.php?api_key=14c88397ab04c3f51ebe81ccfde020cfd4fbc5c8&db=999&output_type=2&url="
                + HttpUtility.UrlEncode(urls.First());
            var body = new MessageBody();
            try
            {
                string jsonStr = await httpClient.GetStringAsync(api_url);
                // Logger.Info(jsonStr);
                var json = JObject.Parse(jsonStr);
                var header = json["results"]![0]!["header"]!;
                var data = json["results"]![0]!["data"]!;
                string similarity = header.Value<string>("similarity")!;
                string thumbnail = header.Value<string>("thumbnail")!;
                int index_id = header.Value<int>("index_id");
                string index_name = header.Value<string>("index_name")!;
                body.Add(SoraSegment.Image("https://proxy.truebigsand.top/" + thumbnail));
                body.AddText($"相似度：{similarity}\n");
                body.AddText($"来源：{index_name}\n");
                if (index_id != 18 && index_id != 38) // not nhentai or ehentai
                {
                    string link = data["ext_urls"]![0]!.ToObject<string>()!;
                    body.AddText($"链接：{link}\n");
                }
                if (index_id == 5) // Pixiv
                {
                    string title = data.Value<string>("title")!;
                    int pixiv_id = data.Value<int>("pixiv_id");
                    string member_name = data.Value<string>("member_name")!;
                    int member_id = data.Value<int>("member_id");
                    body.AddText($"标题：{title}\n");
                    body.AddText($"作品ID：{pixiv_id}\n");
                    body.AddText($"画师：{member_name}\n");
                    body.AddText($"画师ID：{member_id}");
                }
                else if (index_id == 18 || index_id == 38) // nhentai or ehentai
                {
                    string source = data.Value<string>("source")!;
                    string jp_name = data.Value<string>("jp_name")!;
                    body.AddText($"标题：{source}\n");
                    body.AddText($"原标题：{jp_name}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(nameof(SearchImageHandler), ex.Message);
                var exceptionDocument = new ExceptionRecord(ex.Source, ex.Message);
                await GlobalData.exceptionsCollection.InsertOneAsync(exceptionDocument);
                await e.Reply($"发生内部错误，错误ID: {exceptionDocument.Id}");
                return;
            }
            await e.Reply(body);
        }
        public static async Task TestHandler(SoraApi api, GroupMessageEventArgs e, string[] args)
        {
            await e.Reply("1");
            e = WaitForNextMessage(e);
            if (e.Message.GetText().Trim() == "2")
            {
                await e.Reply("is 2");
            }
            else
            {
                await e.Reply("not 2");
            }
        }
        public static async Task MessageStructureHandler(SoraApi api, GroupMessageEventArgs e, string[] args)
        {
            await e.Reply("请发送要获取结构的消息");
            e = WaitForNextMessage(e);
            await e.Reply(e.Message.MessageBody.SerializeToJson());
        }
        public static async Task CommandEnableHandler(SoraApi api, GroupMessageEventArgs e, string[] args)
        {
            if (args.Length == 0)
            {
                await ReplyMessage(e, "参数数量错误");
                return;
            }
            var commands = (await GlobalData.commandsCollection.FindAsync(c => c.Keyword == args[0])).ToList();
            if (!commands.Any())
            {
                await ReplyMessage(e, $"未定义指令：{args[0]}");
                return;
            }
            if (commands.First().IsBuiltInCommand)
            {
                await ReplyMessage(e, "内置指令不可修改状态");
            }
            await GlobalData.commandsCollection.UpdateOneAsync(
                command => command.Keyword == args[0],
                Builders<GroupCommandRecord>.Update.Set(c => c.IsEnabled, true)
            );
            await ReplyMessage(e, $"已启用指令：{args[0]}");
        }
        public static async Task CommandDisableHandler(SoraApi api, GroupMessageEventArgs e, string[] args)
        {
            if (args.Length == 0)
            {
                await ReplyMessage(e, "参数数量错误");
                return;
            }
            var commands = (await GlobalData.commandsCollection.FindAsync(c => c.Keyword == args[0])).ToList();
            if (!commands.Any())
            {
                await ReplyMessage(e, $"未定义指令：{args[0]}");
                return;
            }
            if (commands.First().IsBuiltInCommand)
            {
                await ReplyMessage(e, "内置指令不可修改状态");
            }
            await GlobalData.commandsCollection.UpdateOneAsync(
                command => command.Keyword == args[0],
                Builders<GroupCommandRecord>.Update.Set(c => c.IsEnabled, false)
            );
            await ReplyMessage(e, $"已禁用指令：{args[0]}");
        }
        public static async Task CommandStatusHandler(SoraApi api, GroupMessageEventArgs e, string[] args)
        {
            var sb = new StringBuilder();
            sb.AppendLine("指令状态：");

            foreach (var command in (await GlobalData.commandsCollection.FindAsync(c => true)).ToEnumerable())
            {
                if (!command.IsBuiltInCommand)
                {
                    sb.AppendLine($"{command.Keyword}：{(command.IsEnabled ? "已启用" : "已禁用")}");
                }
            }
            await ReplyMessage(e, sb.ToString().TrimEnd());
        }
        private static async Task ReplyMessage(GroupMessageEventArgs e, params SoraSegment[] segments)
            => await e.Reply(new MessageBody(new[] { SoraSegment.Reply(e.Message.MessageId) }.Concat(segments).ToList()));
        //    def get_middle(content: str, before: str, after: str) -> str:
        //i = content.find(before) + len(before)
        //if i == len(before) - 1:
        //    return str()
        //j = i
        //while not content[j:].startswith(after) :
        //    j += 1
        //return content[i:j]
        private static IEnumerable<string> GetAllBetween(string content, string before, string after)
        {
            int i = content.IndexOf(before) + before.Length;
            if (i == before.Length - 1)
            {
                return [];
            }
            int j = i;
            while (j < content.Length && !content[j..].StartsWith(after))
            {
                j++;
            }
            return new[] { content[i..j] }.Concat(GetAllBetween(content[j..], before, after));
        }
    }
    
}
