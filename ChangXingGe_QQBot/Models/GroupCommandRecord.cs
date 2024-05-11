using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangXingGe_QQBot.Models
{
    public class GroupCommandRecord
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Keyword { get; set; }
        public string HandlerName { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsSuperUserNeeded { get; set; }
        public bool IsBuiltInCommand { get; set; }
    }
}
