using Lagrange.Core.Event.EventArg;
using System.Collections.Concurrent;

namespace ChangXingGeRevived.Services;

public class GroupSessionService
{
    private Dictionary<(uint, uint), (AutoResetEvent Session, GroupMessageEvent Data)> groupSession;
    public GroupSessionService()
    {
        groupSession = new();
    }
    public void Update(uint groupUin, uint memberUin, GroupMessageEvent e)
    {
        var identity = ((uint)e.Chain.GroupUin!, e.Chain.GroupMemberInfo!.Uin);
        if (!groupSession.ContainsKey(identity))
        {
            groupSession.Add(identity, new(new AutoResetEvent(false), null!));
        }
        var session = groupSession[identity];
        session.Session.Set();
        session.Data = e;
        groupSession[identity] = session;
    }
    // assume updated before waitone
    public GroupMessageEvent WaitForNextMessage(uint groupUin, uint memberUin)
    {
        var identity = (groupUin, memberUin);
        var are = groupSession[identity].Session;
        are.Reset();
        are.WaitOne();
        return groupSession[identity].Data;
    }
}
