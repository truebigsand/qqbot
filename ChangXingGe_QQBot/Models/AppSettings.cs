namespace ChangXingGe_QQBot;

public class AppSettings
{
    public Dictionary<string, string> ConnectionStrings { get; set; }
    public AppConfig Config { get; set; }
}

public class AppConfig
{
    public string OneBotHost { get; set; }
    public ushort OneBotPort { get; set; }
    public long[] SuperUsers { get; set; }
    public DateTime StartTime { get; set; }
    public string FriendRequestKey { get; set; }
    public int SetuLimit { get; set; }
    public int PersonalMessageRankLimit { get; set; }
}