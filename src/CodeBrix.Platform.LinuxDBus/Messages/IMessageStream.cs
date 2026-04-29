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
interface IMessageStream
{
    public delegate void MessageReceivedHandler<T>(Exception closeReason, Message message, T state);

    void ReceiveMessages<T>(MessageReceivedHandler<T> handler, T state);

    bool TrySendMessage(MessageBuffer message);

    void BecomeMonitor();

    void Close(Exception closeReason);
}