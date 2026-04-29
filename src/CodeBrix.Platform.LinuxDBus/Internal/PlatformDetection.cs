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
using System.Runtime.Versioning;

static class PlatformDetection
{
    [SupportedOSPlatformGuard("windows")]
    public static bool IsWindows() =>
        // IsWindows is marked with the NonVersionable attribute.
        // This allows R2R to inline it and eliminate platform-specific branches.
        OperatingSystem.IsWindows();
}