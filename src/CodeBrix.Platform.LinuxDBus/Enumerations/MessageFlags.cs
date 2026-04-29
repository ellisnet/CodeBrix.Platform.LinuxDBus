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
/// <summary>Message Flags.</summary>
[Flags]
public enum MessageFlags : byte
{
    /// <summary>None.</summary>
    None = 0,
    /// <summary>No Reply Expected.</summary>
    NoReplyExpected = 1,
    /// <summary>No Auto Start.</summary>
    NoAutoStart = 2,
    /// <summary>Allow Interactive Authorization.</summary>
    AllowInteractiveAuthorization = 4
}