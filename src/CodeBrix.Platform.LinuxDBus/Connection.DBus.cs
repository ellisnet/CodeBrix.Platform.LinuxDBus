using System.Threading.Channels;
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
public partial class Connection
{
    /// <summary>D Bus Object Path.</summary>
    public const string DBusObjectPath = "/org/freedesktop/DBus";
    /// <summary>D Bus Service Name.</summary>
    public const string DBusServiceName = "org.freedesktop.DBus";
    /// <summary>D Bus Interface.</summary>
    public const string DBusInterface = "org.freedesktop.DBus";

    /// <summary>List Services Async.</summary>
    public Task<string[]> ListServicesAsync()
    {
        return CallMethodAsync(CreateMessage(), (Message m, object s) => m.GetBodyReader().ReadArrayOfString());
        MessageBuffer CreateMessage()
        {
            using var writer = GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: DBusServiceName,
                path: DBusObjectPath,
                @interface: DBusInterface,
                member: "ListNames");
            return writer.CreateMessage();
        }
    }

    /// <summary>List Activatable Services Async.</summary>
    public Task<string[]> ListActivatableServicesAsync()
    {
        return CallMethodAsync(CreateMessage(), (Message m, object s) => m.GetBodyReader().ReadArrayOfString());
        MessageBuffer CreateMessage()
        {
            using var writer = GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: DBusServiceName,
                path: DBusObjectPath,
                @interface: DBusInterface,
                member: "ListActivatableNames");
            return writer.CreateMessage();
        }
    }

    /// <summary>Become Monitor Async.</summary>
    public async Task BecomeMonitorAsync(Action<Exception, DisposableMessage> handler, IEnumerable<MatchRule> rules = null)
    {
        if (_connectionOptions.IsShared)
        {
            throw new InvalidOperationException("Cannot become monitor on a shared connection.");
        }

        DBusConnection connection = await ConnectCoreAsync().ConfigureAwait(false);
        await connection.BecomeMonitorAsync(handler, rules).ConfigureAwait(false);
    }

    /// <summary>Monitor Bus Async.</summary>
    public static async IAsyncEnumerable<DisposableMessage> MonitorBusAsync(string address, IEnumerable<MatchRule> rules = null, [EnumeratorCancellation]CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var channel = Channel.CreateUnbounded<DisposableMessage>(
            new UnboundedChannelOptions()
            {
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = true,
            }
        );

        using var connection = new Connection(address);
        using CancellationTokenRegistration ctr =
#if NETCOREAPP3_1_OR_GREATER
                ct.UnsafeRegister(c => ((Connection)c).Dispose(), connection);
#else
                ct.Register(c => ((Connection)c).Dispose(), connection);
#endif
        try
        {
            await connection.ConnectAsync().ConfigureAwait(false);

            await connection.BecomeMonitorAsync(
                (Exception ex, DisposableMessage message) =>
                {
                    if (ex is not null)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            ex = new OperationCanceledException(ct);
                        }
                        channel.Writer.TryComplete(ex);
                        return;
                    }

                    if (!channel.Writer.TryWrite(message))
                    {
                        message.Dispose();
                    }
                },
                rules
            ).ConfigureAwait(false);
        }
        catch
        {
            ct.ThrowIfCancellationRequested();

            throw;
        }
     
        while (await channel.Reader.WaitToReadAsync().ConfigureAwait(false))
        {
            if (channel.Reader.TryRead(out DisposableMessage msg))
            {
                yield return msg;
            }
        }
    }
}