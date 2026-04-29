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
/// <summary>Message Type.</summary>
public enum MessageType : byte
{
    /// <summary>Method Call.</summary>
    MethodCall = 1,
    /// <summary>Method Return.</summary>
    MethodReturn = 2,
    /// <summary>Error.</summary>
    Error = 3,
    /// <summary>Signal.</summary>
    Signal = 4
}