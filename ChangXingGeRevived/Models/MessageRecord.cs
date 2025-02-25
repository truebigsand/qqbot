﻿using Lagrange.Core.Event;
using Lagrange.Core.Event.EventArg;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ChangXingGeRevived.Models;

public enum MessageSource
{
    Group, Friend
}

public class MessageRecord
{
    [Key]
    public int Id { get; set; }
    public ulong SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public ulong MessageId { get; set; }
    public ulong GroupId { get; set; }
    public string RawText { get; set; } = string.Empty;
    public string? DetailedText { get; set; } = null;
    public DateTime Time { get; set; }
    public MessageSource MessageSource { get; set; }
    public static MessageRecord FromEvent(EventBase e) => e switch
    {
        FriendMessageEvent friendEvent => new MessageRecord
        {
            SenderId = friendEvent.Chain.FriendUin,
            SenderName = friendEvent.Chain.FriendInfo!.Nickname,
            MessageId = friendEvent.Chain.MessageId,
            GroupId = 0,
            RawText = friendEvent.Chain.ToPreviewText(),
            DetailedText = friendEvent.Chain.ToPreviewString(),
            Time = friendEvent.Chain.Time,
            MessageSource = MessageSource.Friend
        },
        GroupMessageEvent groupEvent => new MessageRecord
        {
            SenderId = groupEvent.Chain.GroupMemberInfo!.Uin,
            SenderName = groupEvent.Chain.GroupMemberInfo!.MemberName,
            MessageId = groupEvent.Chain.MessageId,
            GroupId = (uint)groupEvent.Chain.GroupUin!,
            RawText = groupEvent.Chain.ToPreviewString(),
            DetailedText = groupEvent.Chain.ToPreviewText(),
            Time = groupEvent.Chain.Time,
            MessageSource = MessageSource.Group
        },
        _ => throw new NotImplementedException()
    };
}
