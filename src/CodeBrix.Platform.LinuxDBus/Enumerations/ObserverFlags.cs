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
/// <summary>Observer Flags.</summary>
[Flags]
public enum ObserverFlags
{
    /// <summary>None.</summary>
    None = 0,
    /// <summary>Emit On Connection Dispose.</summary>
    EmitOnConnectionDispose = 1,
    /// <summary>Emit On Observer Dispose.</summary>
    EmitOnObserverDispose = 2,
    /// <summary>No Subscribe.</summary>
    NoSubscribe = 4,

    /// <summary>Emit On Dispose.</summary>
    EmitOnDispose = EmitOnConnectionDispose | EmitOnObserverDispose,
}
