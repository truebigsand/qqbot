using System.ComponentModel.DataAnnotations;

namespace ChangXingGeRevived.Models;

public class SigninRecord
{
    [Key]
    public int Id { get; set; }
    public ulong GroupId { get; set; }
    public ulong SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public DateTime Time { get; set; }
}
