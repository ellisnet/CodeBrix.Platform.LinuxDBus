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

#pragma warning disable CS0618 // Variant is internally used despite being [Obsolete] in Release
namespace CodeBrix.Platform.LinuxDBus; //was previously: Tmds.DBus.Protocol;
/// <summary>Signature.</summary>
public readonly struct Signature
{
    private readonly byte[] _value;

    // note: C# compiler treats these as static data.
    internal static ReadOnlySpan<byte> Byte => new byte[] { (byte)'y' };
    internal static ReadOnlySpan<byte> Boolean => new byte[] { (byte)'b' };
    internal static ReadOnlySpan<byte> Int16 => new byte[] { (byte)'n' };
    internal static ReadOnlySpan<byte> UInt16 => new byte[] { (byte)'q' };
    internal static ReadOnlySpan<byte> Int32 => new byte[] { (byte)'i' };
    internal static ReadOnlySpan<byte> UInt32 => new byte[] { (byte)'u' };
    internal static ReadOnlySpan<byte> Int64 => new byte[] { (byte)'x' };
    internal static ReadOnlySpan<byte> UInt64 => new byte[] { (byte)'t' };
    internal static ReadOnlySpan<byte> Double => new byte[] { (byte)'d' };
    internal static ReadOnlySpan<byte> UnixFd => new byte[] { (byte)'h' };
    internal static ReadOnlySpan<byte> String => new byte[] { (byte)'s' };
    internal static ReadOnlySpan<byte> ObjectPath => new byte[] { (byte)'o' };
    internal static ReadOnlySpan<byte> Sig => new byte[] { (byte)'g' }; // Name can not be the same as enclosing type.
    internal static ReadOnlySpan<byte> Variant => new byte[] { (byte)'v' };

    internal byte[] Data => _value ?? Array.Empty<byte>();

    /// <summary>Initializes a new instance.</summary>
    [Obsolete("Use the constructor that accepts a ReadOnlySpan.")]
    public Signature(string value)
        => _value = Encoding.UTF8.GetBytes(value);

    /// <summary>Initializes a new instance.</summary>
    public Signature(ReadOnlySpan<byte> value)
        => _value = value.ToArray();

    /// <summary>To String.</summary>
    public override string ToString()
        => Encoding.UTF8.GetString(Data);

    /// <summary>implicit operator Signature.</summary>
    public static implicit operator Signature(ReadOnlySpan<byte> value)
        => new Signature(value);

    /// <summary>As Variant.</summary>
    public Variant AsVariant() => new Variant(this);
}