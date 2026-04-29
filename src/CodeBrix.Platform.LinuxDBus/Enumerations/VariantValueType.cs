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
/// <summary>Variant Value Type.</summary>
public enum VariantValueType : byte
{
    /// <summary>Invalid.</summary>
    Invalid = 0,
    /// <summary>Byte.</summary>
    Byte = DBusType.Byte,
    /// <summary>Bool.</summary>
    Bool = DBusType.Bool,
    /// <summary>Int16.</summary>
    Int16 = DBusType.Int16,
    /// <summary>U Int16.</summary>
    UInt16 = DBusType.UInt16,
    /// <summary>Int32.</summary>
    Int32 = DBusType.Int32,
    /// <summary>U Int32.</summary>
    UInt32 = DBusType.UInt32,
    /// <summary>Int64.</summary>
    Int64 = DBusType.Int64,
    /// <summary>U Int64.</summary>
    UInt64 = DBusType.UInt64,
    /// <summary>Double.</summary>
    Double = DBusType.Double,
    /// <summary>String.</summary>
    String = DBusType.String,
    /// <summary>Object Path.</summary>
    ObjectPath = DBusType.ObjectPath,
    /// <summary>Signature.</summary>
    Signature = DBusType.Signature,
    /// <summary>Array.</summary>
    Array = DBusType.Array,
    /// <summary>Struct.</summary>
    Struct = DBusType.Struct,
    /// <summary>Dictionary.</summary>
    Dictionary = DBusType.DictEntry,
    /// <summary>Unix Fd.</summary>
    UnixFd = DBusType.UnixFd,
    /// <summary>Variant.</summary>
    Variant = DBusType.Variant,
}