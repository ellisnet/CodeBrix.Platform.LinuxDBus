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
public ref partial struct MessageWriter
{
    /// <summary>Write Handle.</summary>
    public void WriteHandle(SafeHandle value)
    {
        int idx = HandleCount;
        AddHandle(value);
        WriteInt32(idx);
    }

    /// <summary>Write Variant Handle.</summary>
    public void WriteVariantHandle(SafeHandle value)
    {
        WriteSignature(Signature.UnixFd);
        WriteHandle(value);
    }

    private int HandleCount => _handles?.Count ?? 0;

    private void AddHandle(SafeHandle handle)
    {
        if (_handles is null)
        {
            _handles = new(isRawHandleCollection: false);
        }
        _handles.AddHandle(handle);
    }
}
