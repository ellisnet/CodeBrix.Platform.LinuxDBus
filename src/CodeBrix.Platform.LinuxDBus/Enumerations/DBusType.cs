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
/// <summary>D Bus Type.</summary>
public enum DBusType : byte
{
    /// <summary>Invalid.</summary>
    Invalid = 0,
    /// <summary>Byte.</summary>
    Byte = (byte)'y',
    /// <summary>Bool.</summary>
    Bool = (byte)'b',
    /// <summary>Int16.</summary>
    Int16 = (byte)'n',
    /// <summary>U Int16.</summary>
    UInt16 = (byte)'q',
    /// <summary>Int32.</summary>
    Int32 = (byte)'i',
    /// <summary>U Int32.</summary>
    UInt32 = (byte)'u',
    /// <summary>Int64.</summary>
    Int64 = (byte)'x',
    /// <summary>U Int64.</summary>
    UInt64 = (byte)'t',
    /// <summary>Double.</summary>
    Double = (byte)'d',
    /// <summary>String.</summary>
    String = (byte)'s',
    /// <summary>Object Path.</summary>
    ObjectPath = (byte)'o',
    /// <summary>Signature.</summary>
    Signature = (byte)'g',
    /// <summary>Array.</summary>
    Array = (byte)'a',
    /// <summary>Struct.</summary>
    Struct = (byte)'(',
    /// <summary>Variant.</summary>
    Variant = (byte)'v',
    /// <summary>Dict Entry.</summary>
    DictEntry = (byte)'{',
    /// <summary>Unix Fd.</summary>
    UnixFd = (byte)'h',
}