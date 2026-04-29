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
/// <summary>I Method Handler.</summary>
public interface IMethodHandler
{
    // Path that is handled by this method handler.
    /// <summary>Path.</summary>
    string Path { get; }

    // The message argument is only valid during the call. It must not be stored to extend its lifetime.
    /// <summary>Handle Method Async.</summary>
    ValueTask HandleMethodAsync(MethodContext context);

    // Controls whether to wait for the handler method to finish executing before reading more messages.
    /// <summary>Run Method Handler Synchronously.</summary>
    bool RunMethodHandlerSynchronously(Message message);
}
