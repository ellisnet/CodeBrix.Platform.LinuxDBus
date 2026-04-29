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
static class ThrowHelper
{
    public static void ThrowIfDisposed(bool condition, object instance)
    {
        if (condition)
        {
            ThrowObjectDisposedException(instance);
        }
    }

    private static void ThrowObjectDisposedException(object instance)
    {
        throw new ObjectDisposedException(instance?.GetType().FullName);
    }

    public static void ThrowIndexOutOfRange()
    {
        throw new IndexOutOfRangeException();
    }

    public static void ThrowNotSupportedException()
    {
        throw new NotSupportedException();
    }

    internal static void ThrowUnexpectedSignature(ReadOnlySpan<byte> signature, string expected)
    {
        throw new ProtocolException($"Expected signature '{expected}' does not match actual signature '{Encoding.UTF8.GetString(signature)}'.");
    }

    [DoesNotReturn]
    internal static void ThrowConnectionClosedByPeer()
    {
        throw new IOException("Connection closed by peer");
    }
}