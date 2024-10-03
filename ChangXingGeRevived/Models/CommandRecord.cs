using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace ChangXingGeRevived.Models;

public class CommandRecord
{
    [BsonId]
    [Key]
    public ObjectId Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public string HandlerName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsSuperUserNeeded { get; set; }
    public bool IsBuiltInCommand { get; set; }
}
