using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangXingGe_QQBot.Models
{
    public class ExceptionRecord
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
        public ExceptionRecord(string source, string message)
        {
            Source = source;
            Message = message;
        }
    }
}
