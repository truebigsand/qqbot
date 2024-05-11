using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Linq;
using MongoDB.Bson.Serialization;
using Sora.Entities.Segment.DataModel;

namespace ChangXingGe_QQBot.Models
{
    public enum MessageSource
    {
        Group, Private
    }
    public class MessageRecord
    {
        public long SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public int MessageId { get; set; }
        public long GroupId { get; set; }
        public string RawText { get; set; } = string.Empty;
        //public IEnumerable<SoraSegment> MessageBody { get; set; } = new List<SoraSegment>();
        public DateTime Time { get; set; }
        public MessageSource MessageSource { get; set; }
        public static MessageRecord FromMessageEventArgs(GroupMessageEventArgs e)
        {
            MessageRecord record = new MessageRecord();
            record.SenderId = e.SenderInfo.UserId;
            record.SenderName = e.SenderInfo.Nick;
            record.MessageId = e.Message.MessageId;
            record.RawText = e.Message.RawText;
            record.GroupId = e.SourceGroup.Id;
            //record.MessageBody = e.Message.MessageBody;
            record.Time = e.Time;
            record.MessageSource = e.EventName == "group" ? MessageSource.Group : MessageSource.Private;
            return record;
        }
        public static MessageRecord FromMessageEventArgs(PrivateMessageEventArgs e)
        {
            MessageRecord record = new MessageRecord();
            record.SenderId = e.SenderInfo.UserId;
            record.SenderName = e.SenderInfo.Nick;
            record.MessageId = e.Message.MessageId;
            record.GroupId = 0;
            record.RawText = e.Message.RawText;
            //record.MessageBody = e.Message.MessageBody;
            record.Time = e.Time;
            record.MessageSource = e.EventName == "group" ? MessageSource.Group : MessageSource.Private;
            return record;
        }
    }
}
