using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nerdbank.Streams;

namespace CodeBrix.Platform.LinuxDBus; //was previously: Tmds.DBus.Protocol;
/// <summary>Method Context.</summary>
public class MethodContext
{
    internal MethodContext(Connection connection, Message request, CancellationToken requestAborted)
    {
        Connection = connection;
        Request = request;
        RequestAborted = requestAborted;
    }

    /// <summary>Request.</summary>
    public Message Request { get; }
    /// <summary>Connection.</summary>
    public Connection Connection { get; }
    /// <summary>Request Aborted.</summary>
    public CancellationToken RequestAborted { get; }

    /// <summary>Reply Sent.</summary>
    public bool ReplySent { get; private set; }

    /// <summary>No Reply Expected.</summary>
    public bool NoReplyExpected => (Request.MessageFlags & MessageFlags.NoReplyExpected) != 0;

    /// <summary>Is D Bus Introspect Request.</summary>
    public bool IsDBusIntrospectRequest { get; internal set; }

    internal List<string> IntrospectChildNameList { get; set; }

    /// <summary>Create Reply Writer.</summary>
    public MessageWriter CreateReplyWriter(string signature)
    {
        var writer = Connection.GetMessageWriter();
        writer.WriteMethodReturnHeader(
            replySerial: Request.Serial,
            destination: Request.Sender,
            signature: signature
        );
        return writer;
    }

    /// <summary>Reply.</summary>
    public void Reply(MessageBuffer message)
    {
        if (ReplySent || NoReplyExpected)
        {
            message.ReturnToPool();
            if (ReplySent)
            {
                throw new InvalidOperationException("A reply has already been sent.");
            }
        }

        ReplySent = true;
        Connection.TrySendMessage(message);
    }

    /// <summary>Reply Error.</summary>
    public void ReplyError(string errorName = null,
                           string errorMsg = null)
    {
        using var writer = Connection.GetMessageWriter();
        writer.WriteError(
            replySerial: Request.Serial,
            destination: Request.Sender,
            errorName: errorName,
            errorMsg: errorMsg
        );
        Reply(writer.CreateMessage());
    }

    /// <summary>Reply Introspect Xml.</summary>
    public void ReplyIntrospectXml(ReadOnlySpan<ReadOnlyMemory<byte>> interfaceXmls)
    {
        if (!IsDBusIntrospectRequest)
        {
            throw new InvalidOperationException($"Can not reply with introspection XML when {nameof(IsDBusIntrospectRequest)} is false.");
        }

        using var writer = Connection.GetMessageWriter();
        writer.WriteMethodReturnHeader(
            replySerial: Request.Serial,
            destination: Request.Sender,
            signature: "s"
        );

        // Add the Peer and Introspectable interfaces.
        // Tools like D-Feet will list the paths separately as soon as there is an interface.
        // We add the base interfaces only for the paths that we want to show up.
        // Those are paths that have other interfaces, paths that are leaves.
        bool includeBaseInterfaces = !interfaceXmls.IsEmpty || IntrospectChildNameList is null || IntrospectChildNameList.Count == 0;
        ReadOnlySpan<ReadOnlyMemory<byte>> baseInterfaceXmls = includeBaseInterfaces ? [ IntrospectionXml.DBusIntrospectable, IntrospectionXml.DBusPeer ] : [ ];

        // Add the child names.
        ReadOnlySpan<string> childNames = CollectionsMarshal.AsSpan(IntrospectChildNameList);
        IEnumerable<string> childNamesEnumerable = null;

        writer.WriteIntrospectionXml(interfaceXmls, baseInterfaceXmls, childNames, childNamesEnumerable);

        Reply(writer.CreateMessage());
    }
}