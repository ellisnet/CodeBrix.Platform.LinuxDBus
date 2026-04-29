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
public ref partial struct Reader
{
    /// <summary>Read Byte.</summary>
    public byte ReadByte()
    {
        if (!_reader.TryRead(out byte b))
        {
            ThrowHelper.ThrowIndexOutOfRange();
        }
        return b;
    }

    /// <summary>Read Bool.</summary>
    public bool ReadBool()
    {
        return ReadInt32() != 0;
    }

    /// <summary>Read U Int16.</summary>
    public ushort ReadUInt16()
        => (ushort)ReadInt16();

    /// <summary>Read Int16.</summary>
    public short ReadInt16()
    {
        AlignReader(alignment: 2);
        bool dataRead = _isBigEndian ? _reader.TryReadBigEndian(out short rv) : _reader.TryReadLittleEndian(out rv);
        if (!dataRead)
        {
            ThrowHelper.ThrowIndexOutOfRange();
        }
        return rv;
    }

    /// <summary>Read U Int32.</summary>
    public uint ReadUInt32()
        => (uint)ReadInt32();

    /// <summary>Read Int32.</summary>
    public int ReadInt32()
    {
        AlignReader(alignment: 4);
        bool dataRead = _isBigEndian ? _reader.TryReadBigEndian(out int rv) : _reader.TryReadLittleEndian(out rv);
        if (!dataRead)
        {
            ThrowHelper.ThrowIndexOutOfRange();
        }
        return rv;
    }

    /// <summary>Read U Int64.</summary>
    public ulong ReadUInt64()
        => (ulong)ReadInt64();

    /// <summary>Read Int64.</summary>
    public long ReadInt64()
    {
        AlignReader(alignment: 8);
        bool dataRead = _isBigEndian ? _reader.TryReadBigEndian(out long rv) : _reader.TryReadLittleEndian(out rv);
        if (!dataRead)
        {
            ThrowHelper.ThrowIndexOutOfRange();
        }
        return rv;
    }

    /// <summary>Read Double.</summary>
    public unsafe double ReadDouble()
    {
        double value;
        *(long*)&value = ReadInt64();
        return value;
    }

    /// <summary>Read Signature.</summary>
    public Signature ReadSignature()
        => new Signature(ReadSignatureAsSpan());

    /// <summary>Read Signature As Span.</summary>
    public ReadOnlySpan<byte> ReadSignatureAsSpan()
    {
        int length = ReadByte();
        return ReadSpan(length);
    }

    /// <summary>Read Signature.</summary>
    public void ReadSignature(string expected)
    {
        ReadOnlySpan<byte> signature = ReadSignatureAsSpan();
        if (signature.Length != expected.Length)
        {
            ThrowHelper.ThrowUnexpectedSignature(signature, expected);
        }
        for (int i = 0; i < signature.Length; i++)
        {
            if (signature[i] != expected[i])
            {
                ThrowHelper.ThrowUnexpectedSignature(signature, expected);
            }
        }
    }

    /// <summary>Read Signature.</summary>
    public void ReadSignature(ReadOnlySpan<byte> expected)
    {
        ReadOnlySpan<byte> signature = ReadSignatureAsSpan();
        if (!signature.SequenceEqual(expected))
        {
            ThrowHelper.ThrowUnexpectedSignature(signature, Encoding.UTF8.GetString(expected));
        }
    }

    /// <summary>Read Object Path As Span.</summary>
    public ReadOnlySpan<byte> ReadObjectPathAsSpan() => ReadSpan();

    /// <summary>Read Object Path.</summary>
    public ObjectPath ReadObjectPath() => new ObjectPath(ReadString());

    /// <summary>Read Object Path As String.</summary>
    public string ReadObjectPathAsString() => ReadString();

    /// <summary>Read String As Span.</summary>
    public ReadOnlySpan<byte> ReadStringAsSpan() => ReadSpan();

    /// <summary>Read String.</summary>
    public string ReadString() => Encoding.UTF8.GetString(ReadSpan());

    /// <summary>Read Signature As String.</summary>
    public string ReadSignatureAsString() => Encoding.UTF8.GetString(ReadSignatureAsSpan());

    private ReadOnlySpan<byte> ReadSpan()
    {
        int length = (int)ReadUInt32();
        return ReadSpan(length);
    }

    private ReadOnlySpan<byte> ReadSpan(int length)
    {
        var span = _reader.UnreadSpan;
        if (span.Length >= length)
        {
            _reader.Advance(length + 1);
            return span.Slice(0, length);
        }
        else
        {
            var buffer = new byte[length];
            if (!_reader.TryCopyTo(buffer))
            {
                ThrowHelper.ThrowIndexOutOfRange();
            }
            _reader.Advance(length + 1);
            return new ReadOnlySpan<byte>(buffer);
        }
    }

    private bool ReverseEndianness
        => BitConverter.IsLittleEndian != !_isBigEndian;
}
