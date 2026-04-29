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
sealed class CloseSafeHandle : SafeHandle
{
    public CloseSafeHandle() :
        base(new IntPtr(-1), ownsHandle: true)
    { }

    public override bool IsInvalid
        => handle == new IntPtr(-1);

    protected override bool ReleaseHandle()
        => close(handle.ToInt32()) == 0;

    [DllImport("libc", SetLastError = true)]
    internal static extern int close(int fd);
}
