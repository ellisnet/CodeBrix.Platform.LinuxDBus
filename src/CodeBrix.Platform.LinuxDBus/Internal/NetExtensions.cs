using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace CodeBrix.Platform.LinuxDBus; //was previously: Tmds.DBus.Protocol.NetstandardExtensions (NET branch);

internal static partial class NetstandardExtensions
{
    public static SafeHandle GetSafeHandle(this Socket socket)
        => socket.SafeHandle;

    public static string AsString(this ReadOnlySpan<char> chars)
        => new string(chars);

    public static string AsString(this Span<char> chars)
        => AsString((ReadOnlySpan<char>)chars);
}
