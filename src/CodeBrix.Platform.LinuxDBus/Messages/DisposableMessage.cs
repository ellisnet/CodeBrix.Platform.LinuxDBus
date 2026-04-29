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
/// <summary>Disposable Message.</summary>
public struct DisposableMessage : IDisposable
{
    private Message _message;

    internal DisposableMessage(Message message)
        => _message = message;

    /// <summary>Message.</summary>
    public Message Message
        => _message ?? throw new ObjectDisposedException(typeof(Message).FullName);

    /// <summary>Dispose.</summary>
    public void Dispose()
    {
        _message?.ReturnToPool();
        _message = null;
    }
}
