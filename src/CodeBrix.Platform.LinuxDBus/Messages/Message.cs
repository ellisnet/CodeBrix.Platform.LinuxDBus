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
/// <summary>Message.</summary>
public sealed class Message
{
    private const int HeaderFieldsLengthOffset = 12;

    private readonly MessagePool _pool;
    private readonly Sequence<byte> _data;

    private UnixFdCollection _handles;
    private ReadOnlySequence<byte> _body;

    /// <summary>Is Big Endian.</summary>
    public bool IsBigEndian { get; private set; }
    /// <summary>Serial.</summary>
    public uint Serial { get; private set; }
    /// <summary>Message Flags.</summary>
    public MessageFlags MessageFlags { get; private set; }
    /// <summary>Message Type.</summary>
    public MessageType MessageType { get; private set; }

    /// <summary>Reply Serial.</summary>
    public uint? ReplySerial { get; private set; }
    /// <summary>Unix Fd Count.</summary>
    public int UnixFdCount { get; private set; }

    private HeaderBuffer _path;
    private HeaderBuffer _interface;
    private HeaderBuffer _member;
    private HeaderBuffer _errorName;
    private HeaderBuffer _destination;
    private HeaderBuffer _sender;
    private HeaderBuffer _signature;

    /// <summary>Path As String.</summary>
    public string PathAsString => _path.ToString();
    /// <summary>Interface As String.</summary>
    public string InterfaceAsString => _interface.ToString();
    /// <summary>Member As String.</summary>
    public string MemberAsString => _member.ToString();
    /// <summary>Error Name As String.</summary>
    public string ErrorNameAsString => _errorName.ToString();
    /// <summary>Destination As String.</summary>
    public string DestinationAsString => _destination.ToString();
    /// <summary>Sender As String.</summary>
    public string SenderAsString => _sender.ToString();
    /// <summary>Signature As String.</summary>
    public string SignatureAsString => _signature.ToString();

    /// <summary>Path.</summary>
    public ReadOnlySpan<byte> Path => _path.Span;
    /// <summary>Interface.</summary>
    public ReadOnlySpan<byte> Interface => _interface.Span;
    /// <summary>Member.</summary>
    public ReadOnlySpan<byte> Member => _member.Span;
    /// <summary>Error Name.</summary>
    public ReadOnlySpan<byte> ErrorName => _errorName.Span;
    /// <summary>Destination.</summary>
    public ReadOnlySpan<byte> Destination => _destination.Span;
    /// <summary>Sender.</summary>
    public ReadOnlySpan<byte> Sender => _sender.Span;
    /// <summary>Signature.</summary>
    public ReadOnlySpan<byte> Signature => _signature.Span;

    /// <summary>Path Is Set.</summary>
    public bool PathIsSet => _path.IsSet;
    /// <summary>Interface Is Set.</summary>
    public bool InterfaceIsSet => _interface.IsSet;
    /// <summary>Member Is Set.</summary>
    public bool MemberIsSet => _member.IsSet;
    /// <summary>Error Name Is Set.</summary>
    public bool ErrorNameIsSet => _errorName.IsSet;
    /// <summary>Destination Is Set.</summary>
    public bool DestinationIsSet => _destination.IsSet;
    /// <summary>Sender Is Set.</summary>
    public bool SenderIsSet => _sender.IsSet;
    /// <summary>Signature Is Set.</summary>
    public bool SignatureIsSet => _signature.IsSet;

    struct HeaderBuffer
    {
        private byte[] _buffer;
        private int _length;
        private string _string;

        public Span<byte> Span => new Span<byte>(_buffer, 0, Math.Max(_length, 0));

        public void Set(ReadOnlySpan<byte> data)
        {
            _string = null;
            if (_buffer is null || data.Length > _buffer.Length)
            {
                _buffer = new byte[data.Length];
            }
            data.CopyTo(_buffer);
            _length = data.Length;
        }

        public void Clear()
        {
            _length = -1;
            _string = null;
        }

        public override string ToString()
        {
            return _length == -1 ? null : _string ??= Encoding.UTF8.GetString(Span);
        }

        public bool IsSet => _length != -1;
    }

    /// <summary>Get Body Reader.</summary>
    public Reader GetBodyReader() => new Reader(IsBigEndian, _body, _handles, UnixFdCount);

    internal Message(MessagePool messagePool, Sequence<byte> sequence)
    {
        _pool = messagePool;
        _data = sequence;
        ClearHeaders();
    }

    internal void ReturnToPool()
    {
        _data.Reset();
        ClearHeaders();
        _handles?.Dispose();
        _handles = null;
        _pool.Return(this);
    }

    private void ClearHeaders()
    {
        ReplySerial = null;
        UnixFdCount = 0;

        _path.Clear();
        _interface.Clear();
        _member.Clear();
        _errorName.Clear();
        _destination.Clear();
        _sender.Clear();
        _signature.Clear();
    }

    internal static Message TryReadMessage(MessagePool messagePool, ref ReadOnlySequence<byte> sequence, UnixFdCollection handles = null, bool isMonitor = false)
    {
        SequenceReader<byte> seqReader = new(sequence);
        if (!seqReader.TryRead(out byte endianness) ||
            !seqReader.TryRead(out byte msgType) ||
            !seqReader.TryRead(out byte flags) ||
            !seqReader.TryRead(out byte version))
        {
            return null;
        }

        if (version != 1)
        {
            throw new NotSupportedException();
        }

        bool isBigEndian = endianness == 'B';

        if (!TryReadUInt32(ref seqReader, isBigEndian, out uint bodyLength) ||
            !TryReadUInt32(ref seqReader, isBigEndian, out uint serial) ||
            !TryReadUInt32(ref seqReader, isBigEndian, out uint headerFieldLength))
        {
            return null;
        }

        headerFieldLength = (uint)ProtocolConstants.Align((int)headerFieldLength, ProtocolConstants.StructAlignment);

        long totalLength = seqReader.Consumed + headerFieldLength + bodyLength;

        if (sequence.Length < totalLength)
        {
            return null;
        }

        // Copy data so it has a lifetime independent of the source sequence.
        var message = messagePool.Rent();
        Sequence<byte> dst = message._data;
        do
        {
            ReadOnlySpan<byte> srcSpan = sequence.First.Span;
            int length = (int)Math.Min(totalLength, srcSpan.Length);
            Span<byte> dstSpan = dst.GetSpan(0);
            length = Math.Min(length, dstSpan.Length);
            srcSpan.Slice(0, length).CopyTo(dstSpan);
            dst.Advance(length);
            sequence = sequence.Slice(length);
            totalLength -= length;
        } while (totalLength > 0);

        message.IsBigEndian = isBigEndian;
        message.Serial = serial;
        message.MessageType = (MessageType)msgType;
        message.MessageFlags = (MessageFlags)flags;
        message.ParseHeader(handles, isMonitor);

        return message;

        static bool TryReadUInt32(ref SequenceReader<byte> seqReader, bool isBigEndian, out uint value)
        {
            int v;
            bool rv = (isBigEndian && seqReader.TryReadBigEndian(out v) || seqReader.TryReadLittleEndian(out v));
            value = (uint)v;
            return rv;
        }
    }

    private void ParseHeader(UnixFdCollection handles, bool isMonitor)
    {
        var reader = new Reader(IsBigEndian, _data.AsReadOnlySequence);
        reader.Advance(HeaderFieldsLengthOffset);

        ArrayEnd headersEnd = reader.ReadArrayStart(ProtocolConstants.StructAlignment);
        while (reader.HasNext(headersEnd))
        {
            MessageHeader hdrType = (MessageHeader)reader.ReadByte();
            ReadOnlySpan<byte> sig = reader.ReadSignatureAsSpan();
            switch (hdrType)
            {
                case MessageHeader.Path:
                    _path.Set(reader.ReadObjectPathAsSpan());
                    break;
                case MessageHeader.Interface:
                    _interface.Set(reader.ReadStringAsSpan());
                    break;
                case MessageHeader.Member:
                    _member.Set(reader.ReadStringAsSpan());
                    break;
                case MessageHeader.ErrorName:
                    _errorName.Set(reader.ReadStringAsSpan());
                    break;
                case MessageHeader.ReplySerial:
                    ReplySerial = reader.ReadUInt32();
                    break;
                case MessageHeader.Destination:
                    _destination.Set(reader.ReadStringAsSpan());
                    break;
                case MessageHeader.Sender:
                    _sender.Set(reader.ReadStringAsSpan());
                    break;
                case MessageHeader.Signature:
                    _signature.Set(reader.ReadSignatureAsSpan());
                    break;
                case MessageHeader.UnixFds:
                    UnixFdCount = (int)reader.ReadUInt32();
                    if (UnixFdCount > 0 && !isMonitor)
                    {
                        if (handles is null || UnixFdCount > handles.Count)
                        {
                            throw new ProtocolException("Received less handles than UNIX_FDS.");
                        }
                        if (_handles is null)
                        {
                            _handles = new(handles.IsRawHandleCollection);
                        }
                        handles.MoveTo(_handles, UnixFdCount);
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
        reader.AlignStruct();

        _body = reader.UnreadSequence;
    }
}