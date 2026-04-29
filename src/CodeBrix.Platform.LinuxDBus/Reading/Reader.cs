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
    private readonly bool _isBigEndian;
    private readonly UnixFdCollection _handles;
    private readonly int _handleCount;
    private SequenceReader<byte> _reader;

    internal ReadOnlySequence<byte> UnreadSequence => _reader.Sequence.Slice(_reader.Position);

    internal void Advance(long count) => _reader.Advance(count);

    internal Reader(bool isBigEndian, ReadOnlySequence<byte> sequence) : this(isBigEndian, sequence, handles: null, 0) { }

    internal Reader(bool isBigEndian, ReadOnlySequence<byte> sequence, UnixFdCollection handles, int handleCount)
    {
        _reader = new(sequence);

        _isBigEndian = isBigEndian;
        _handles = handles;
        _handleCount = handleCount;
    }

    /// <summary>Align Struct.</summary>
    public void AlignStruct()
        => AlignReader(ProtocolConstants.StructAlignment);

    private void AlignReader(int alignment)
    {
        long pad = ProtocolConstants.GetPadding((int)_reader.Consumed, alignment);
        if (pad != 0)
        {
            _reader.Advance(pad);
        }
    }

    /// <summary>Read Array Start.</summary>
    public ArrayEnd ReadArrayStart(DBusType elementType)
        => ReadArrayStart(ProtocolConstants.GetTypeAlignment(elementType));

    internal ArrayEnd ReadArrayStart(int alignment)
    {
        uint arrayLength = ReadUInt32();
        AlignReader(alignment);
        int endOfArray = (int)(_reader.Consumed + arrayLength);
        return new ArrayEnd(alignment, endOfArray);
    }

    /// <summary>Has Next.</summary>
    public bool HasNext(ArrayEnd iterator)
    {
        int consumed = (int)_reader.Consumed;
        int nextElement = ProtocolConstants.Align(consumed, iterator.Alignment);
        if (nextElement >= iterator.EndOfArray)
        {
            return false;
        }
        int advance = nextElement - consumed;
        if (advance != 0)
        {
            _reader.Advance(advance);
        }
        return true;
    }

    /// <summary>Skip To.</summary>
    public void SkipTo(ArrayEnd end)
    {
        int advance = end.EndOfArray - (int)_reader.Consumed;
        _reader.Advance(advance);
    }
}

/// <summary>Array End.</summary>
public ref struct ArrayEnd
{
    internal readonly int Alignment;
    internal readonly int EndOfArray;

    internal ArrayEnd(int alignment, int endOfArray)
    {
        Alignment = alignment;
        EndOfArray = endOfArray;
    }
}