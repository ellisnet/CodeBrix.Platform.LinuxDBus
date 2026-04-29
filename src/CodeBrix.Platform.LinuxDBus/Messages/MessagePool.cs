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
class MessagePool
{
    private const int MinimumSpanLength = 512;

    private Message _pooled = null; // Pool a single message.

    public Message Rent()
    {
        Message rent = Interlocked.Exchange(ref _pooled, null);

        if (rent is not null)
        {
            return rent;
        }

        var sequence = new Sequence<byte>(ArrayPool<byte>.Shared) { MinimumSpanLength = MinimumSpanLength };

        return new Message(this, sequence);
    }

    internal void Return(Message value)
    {
        Volatile.Write(ref _pooled, value);
    }
}
