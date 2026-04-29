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
/// <summary>Connect Exception.</summary>
public class ConnectException : Exception
{
    /// <summary>Initializes a new instance.</summary>
    public ConnectException(string message) : base(message)
    { }

    /// <summary>Initializes a new instance.</summary>
    public ConnectException(string message, Exception innerException) : base(message, innerException)
    { }
}