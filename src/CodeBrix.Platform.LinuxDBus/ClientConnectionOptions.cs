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
/// <summary>Client Connection Options.</summary>
public class ClientConnectionOptions : ConnectionOptions
{
    private string _address;

    /// <summary>Initializes a new instance.</summary>
    public ClientConnectionOptions(string address)
    {
        if (address == null)
            throw new ArgumentNullException(nameof(address));
        _address = address;
    }

    /// <summary>Initializes a new instance.</summary>
    protected internal ClientConnectionOptions()
    {
        _address = string.Empty;
    }

    /// <summary>Auto Connect.</summary>
    public bool AutoConnect { get; set; }

    internal bool IsShared { get; set; }

    /// <summary>Setup Async.</summary>
    protected internal virtual ValueTask<ClientSetupResult> SetupAsync(CancellationToken cancellationToken)
    {
        return new ValueTask<ClientSetupResult>(
            new ClientSetupResult(_address)
            {
                SupportsFdPassing = true,
                UserId = DBusEnvironment.UserId,
                MachineId = DBusEnvironment.MachineId
            });
    }

    /// <summary>Teardown.</summary>
    protected internal virtual void Teardown(object token)
    { }
}