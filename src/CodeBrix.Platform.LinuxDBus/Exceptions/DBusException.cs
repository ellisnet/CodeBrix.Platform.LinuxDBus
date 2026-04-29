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
/// <summary>D Bus Exception.</summary>
public class DBusException : Exception
{
    /// <summary>Initializes a new instance.</summary>
    public DBusException(string errorName, string errorMessage) :
        base($"{errorName}: {errorMessage}")
    {
        ErrorName = errorName;
        ErrorMessage = errorMessage;
    }

    /// <summary>Error Name.</summary>
    public string ErrorName { get; }

    /// <summary>Error Message.</summary>
    public string ErrorMessage { get; }
}
