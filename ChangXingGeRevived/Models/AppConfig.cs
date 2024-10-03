namespace ChangXingGeRevived.Models;

public class AppConfig
{
    public QQBotConfig BotConfig { get; set; } = null!;
    public string DeviceInfoPath { get; set; } = null!;
    public string KeystorePath { get; set; } = null!;
}

public class QQBotConfig
{
    public uint[] SuperUsers { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public string FriendRequestKey { get; set; } = null!;
    public int SetuLimit { get; set; }
    public int PersonalMessageRankLimit { get; set; }
    //public uint? Uin { get; set; } = null;
    //public string? Password {  get; set; } = null;
}
