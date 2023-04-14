using Chaldene.Data.Messages.Concretes;
using Chaldene.Data.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qqbot
{
    public static class MessageChainExtensions
    {
        public static string ToReadableString(this IEnumerable<MessageBase> messages)
        {
            var sb = new StringBuilder();
            foreach (var message in messages)
            {
                sb.Append(message switch
                {
                    PlainMessage msg => msg.Text,
                    AtMessage msg => "@" + msg.Target,
                    AtAllMessage msg => "@全体成员",
                    AppMessage msg => msg.Content,
                    DiceMessage msg => $"[骰子:{msg.Value}]",
                    FaceMessage msg => $"[表情:{msg.FaceId}({msg.Name})]",
                    FileMessage msg => $"[文件:{msg.Name}({GetFileSizeString(msg.Size)})({msg.FileId})]",
                    FlashImageMessage msg => $"[闪照:{msg.ImageId}({msg.Url})]",
                    ImageMessage msg => $"[图片:{msg.ImageId}({msg.Url})]",
                    ForwardMessage msg => $"[转发:{string.Join(", ", msg.NodeList.Select(m => ToReadableString(m.MessageChain)))}]",
                    MarketFaceMessage msg => $"[商城表情:{msg.Id}({msg.Name})]",
                    MiraiCodeMessage msg => $"[Mirai码:{msg.Code}]",
                    MusicShareMessage msg => $"[音乐分享:{msg.Title}({msg.MusicUrl})]",
                    PokeMessage msg => $"[Poke:{msg.Name}]",
                    QuoteMessage msg => $"[引用:{msg.SenderId}:{ToReadableString(msg.Origin)}]",
                    VoiceMessage msg => $"[语音:{msg.VoiceId}({msg.Url})]",
                    JsonMessage msg => $"[Json消息:{msg.Json}]",
                    XmlMessage msg => $"[Xml消息:{msg.Xml}]",
                    UnknownMessage msg => $"[未知消息:{msg.RawJson}]",
                    _ => string.Empty
                });
            }
            return sb.ToString();
        }
        private static string GetFileSizeString(long size)
        {
            const long GB = 1024 * 1024 * 1024;
            const long MB = 1024 * 1024;
            const long KB = 1024;
            return size switch
            {
                >= GB => Math.Round(size / (float)GB, 2) + "GB",
                >= MB => Math.Round(size / (float)MB, 2) + "MB",
                >= KB => Math.Round(size / (float)KB, 2) + "KB",
                _ => size + "B"
            };
        }
    }
}
