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
/// <summary>Object Path.</summary>
public readonly struct ObjectPath
{
    private readonly string _value;

    /// <summary>Initializes a new instance.</summary>
    public ObjectPath(string value)
    {
        _value = value;
        ThrowIfEmpty();
    }

    internal void ThrowIfEmpty()
    {
        if (_value is null || _value.Length == 0)
        {
            ThrowEmptyException();
        }
    }

    private void ThrowEmptyException()
    {
        throw new ArgumentException($"{nameof(ObjectPath)} is empty.");
    }

    /// <summary>To String.</summary>
    public override string ToString() => _value ?? "";

    /// <summary>implicit operator string.</summary>
    public static implicit operator string(ObjectPath value) => value._value;

    /// <summary>implicit operator Object Path.</summary>
    public static implicit operator ObjectPath(string value) => new ObjectPath(value);

    /// <summary>As Variant.</summary>
    public Variant AsVariant() => new Variant(this);
}