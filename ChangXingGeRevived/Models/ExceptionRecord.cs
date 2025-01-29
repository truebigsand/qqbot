using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ChangXingGeRevived.Models;

public class ExceptionRecord
{
    [Key]
    public int Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
