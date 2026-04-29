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
/// <summary>Client Setup Result.</summary>
public class ClientSetupResult
{
    /// <summary>Initializes a new instance.</summary>
    public ClientSetupResult(string address)
    {
        ConnectionAddress = address ?? throw new ArgumentNullException(nameof(address));
    }

    /// <summary>Initializes a new instance.</summary>
    public ClientSetupResult() :
        this("")
    { }

    /// <summary>Connection Address.</summary>
    public string ConnectionAddress { get;  }

    /// <summary>Teardown Token.</summary>
    public object TeardownToken { get; set; }

    /// <summary>User Id.</summary>
    public string UserId { get; set; }

    /// <summary>Machine Id.</summary>
    public string MachineId { get; set; }

    /// <summary>Supports Fd Passing.</summary>
    public bool SupportsFdPassing { get; set; }

    // SupportsFdPassing and ConnectionAddress are ignored when this is set.
    // The implementation assumes that it is safe to Dispose the Stream
    // while there are on-going reads/writes, and that these on-going operations will be aborted.
    /// <summary>Connection Stream.</summary>
    public Stream ConnectionStream { get; set; }
}