using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace ChangXingGeRevived.Models;

public class ExceptionRecord
{
    [BsonId]
    [Key]
    public ObjectId Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
