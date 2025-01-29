using Lagrange.Core.Event.EventArg;
using System.Collections.Concurrent;

namespace ChangXingGeRevived.Services;

//public class GroupSessionService
//{
//    private Dictionary<(uint, uint), (AutoResetEvent Session, GroupMessageEvent Data)> groupSession;
//    public GroupSessionService()
//    {
//        groupSession = new();
//    }
//    public void Update(uint groupUin, uint memberUin, GroupMessageEvent e)
//    {
//        var identity = ((uint)e.Chain.GroupUin!, e.Chain.GroupMemberInfo!.Uin);
//        if (!groupSession.ContainsKey(identity))
//        {
//            groupSession.Add(identity, new(new AutoResetEvent(false), null!));
//        }
//        var session = groupSession[identity];
//        var are = session.Session;
//        session.Data = e;
//        groupSession[identity] = session;
//        are.Set();
//    }
//    // assume updated before waitone
//    public GroupMessageEvent WaitForNextMessage(uint groupUin, uint memberUin)
//    {
//        var identity = (groupUin, memberUin);
//        var are = groupSession[identity].Session;
//        are.Reset();
//        are.WaitOne();
//        return groupSession[identity].Data;
//    }
//}

public class GroupSessionService
{
    private Dictionary<(uint, uint), (object Lock, GroupMessageEvent Data)> groupSession;
    public GroupSessionService()
    {
        groupSession = new();
    }
    public void Update(uint groupUin, uint memberUin, GroupMessageEvent e)
    {
        var identity = ((uint)e.Chain.GroupUin!, e.Chain.GroupMemberInfo!.Uin);
        if (!groupSession.ContainsKey(identity))
        {
            groupSession.Add(identity, new(new object(), null!));
        }
        var session = groupSession[identity];
        lock (session.Lock)
        {
            session.Data = e;
            groupSession[identity] = session;
            Monitor.Pulse(session.Lock);
        }
    }
    // assume updated before waitone
    public GroupMessageEvent WaitForNextMessage(uint groupUin, uint memberUin)
    {
        var identity = (groupUin, memberUin);
        var @lock = groupSession[identity].Lock;
        lock (@lock)
        {
            Monitor.Wait(@lock);
            return groupSession[identity].Data;
        }
    }
}