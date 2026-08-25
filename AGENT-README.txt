================================================================================
AGENT-README: CodeBrix.Platform.LinuxDBus
A Guide for AI Coding Agents — CONSUMING the
CodeBrix.Platform.LinuxDBus.MitLicenseForever NuGet package
================================================================================

OVERVIEW
========

CodeBrix.Platform.LinuxDBus is a low-level D-Bus protocol library for Linux,
targeting .NET 10 or later. It speaks the D-Bus wire protocol directly:

  - connects to the session bus, the system bus, or any address string the
    D-Bus specification allows (unix:path=, unix:abstract=, tcp:host=...);
  - sends method calls and awaits their replies;
  - subscribes to signals through match rules;
  - registers method handlers so your process can EXPOSE D-Bus objects;
  - reads and writes every D-Bus wire type: the basic types, arrays,
    dictionaries, structs, variants, object paths, signatures and Unix file
    descriptors;
  - generates D-Bus introspection XML for the object paths you registered.

It is a fully-managed port of the upstream Tmds.DBus.Protocol library
(version 0.21.3) by Tom Deseyn and contributors, re-targeted to .NET 10 and
renamed. The public API shape is preserved; only the namespace changes, from
`Tmds.DBus.Protocol` to `CodeBrix.Platform.LinuxDBus`. DO NOT write
`using Tmds.DBus.Protocol;` in code that consumes this package, and do not
add the upstream package alongside it — the two assemblies define the same
type names and will collide.

This is a PROTOCOL library, not a client framework. There is no source
generator, no `[DBusInterface]` attribute, no generated proxy classes. You
build each outgoing message by writing its header and body fields in order,
and you decode each reply by reading its body fields back in the same order.
The signature string you pass when writing the header must match the values
you then write, and the reads you perform must match the signature the peer
actually sent.


INSTALLATION
============

NuGet PackageId:  CodeBrix.Platform.LinuxDBus.MitLicenseForever

    dotnet add package CodeBrix.Platform.LinuxDBus.MitLicenseForever

IMPORTANT: the package id ends in `.MitLicenseForever`; it is NOT published
as `CodeBrix.Platform.LinuxDBus`. The assembly, the root namespace and the
`Product`/`Title` are all `CodeBrix.Platform.LinuxDBus`.

NuGet dependencies:  none. The package needs only the .NET shared framework.
License:             MIT.

Requirements and limits:

  - .NET 10 or later. There is no netstandard or .NET 8 target.
  - Linux at run time. The library talks to a D-Bus daemon over Unix domain
    sockets (or TCP), and Unix file-descriptor passing uses `libc`.
    `Address.Session` also probes X11 (`libX11`) when
    `DBUS_SESSION_BUS_ADDRESS` is not set.
  - A running D-Bus daemon. Nothing in this package starts one for you.
  - `AllowUnsafeBlocks` is NOT required in your project; the unsafe code is
    inside the library.

The package compiles and loads on Windows and macOS (the assembly is not
OS-gated), but connecting will fail without a reachable bus, and the
file-descriptor paths P/Invoke `libc`. Treat it as Linux-only.


KEY NAMESPACES / USINGS
=======================

    using CodeBrix.Platform.LinuxDBus;

That single `using` covers the entire public surface. Every public type in
the package lives in the `CodeBrix.Platform.LinuxDBus` namespace, no matter
which source sub-folder it came from — there are no sub-namespaces.

Types you will also need from the BCL in typical code:

    using System;                            // IDisposable, Exception
    using System.Collections.Generic;        // Dictionary<,>, KeyValuePair<,>
    using System.Threading.Tasks;            // Task, ValueTask
    using System.Runtime.InteropServices;    // SafeHandle (for Unix fds)


SECURITY-RELEVANT BEHAVIOUR YOU CAN RELY ON
===========================================

This port carries the patched upstream source that fixes advisory
GHSA-xrw6-gwf8-vvr9 / CVE-2026-39959 (High severity, CVSS 7.1). Three
behaviours follow from that, and consumer code should be written knowing
them:

  1. SIGNAL SENDERS ARE VERIFIED. When a `MatchRule.Sender` names a
     well-known bus name, the connection resolves that name to its CURRENT
     unique-name owner and tracks `NameOwnerChanged`; a signal is delivered
     to your handler only when the actual unique-name sender matches. A
     hostile peer cannot impersonate a well-known name by forging the
     `Sender` header. Consequence: always set `MatchRule.Sender` when you
     care who sent a signal — leaving it unset accepts signals from anyone.

  2. FILE-DESCRIPTOR LIMITS ARE ENFORCED. At most 16 Unix file descriptors
     may be attached to one message (the D-Bus specification's cap).
     Received descriptors are made close-on-exec at the moment they are
     received, truncated control messages are rejected, and leftover
     descriptors are never carried from one message into the next. If you
     need to pass more handles than the cap allows, split the work across
     several messages.

  3. EXCEPTIONS FROM YOUR CALLBACKS DO NOT CRASH THE PROCESS — THEY KILL THE
     CONNECTION. A throw inside your `MessageValueReader<T>` or inside your
     signal handler is caught, the connection is disconnected with that
     exception as the reason, and your handler is then invoked once more
     with a `DisconnectedException` whose `InnerException` is what you
     threw. The same is true of a throw out of `IMethodHandler
     .HandleMethodAsync`. Catch your own exceptions inside callbacks unless
     tearing down the connection is what you want.


CORE API REFERENCE
==================

CONNECTION — THE MAIN ENTRY POINT
---------------------------------
`Connection` owns the transport, the read/write pipeline and the
method-dispatch table.

    public partial class Connection : IDisposable

It implements `IDisposable`, NOT `IAsyncDisposable`. Use `using var` /
`connection.Dispose()`. There is no `DisposeAsync`.

Construction:

    public Connection(string address)
    public Connection(ConnectionOptions connectionOptions)

    var connection = new Connection(Address.Session);
    var connection = new Connection(Address.System);
    var connection = new Connection("unix:path=/run/user/1000/bus");
    var connection = new Connection(new ClientConnectionOptions(address)
    {
        AutoConnect = true,
    });

The `ConnectionOptions` overload requires a `ClientConnectionOptions`
instance (it casts internally); `ConnectionOptions` itself is an abstract
base with an internal constructor, so `ClientConnectionOptions` (or your own
subclass of it) is the only thing you can pass.

Shared, process-wide, auto-connecting connections:

    public static Connection System { get; }    // shared, AutoConnect
    public static Connection Session { get; }   // shared, AutoConnect

    public string UniqueName { get; }           // e.g. ":1.42"

Connecting and lifetime:

    public async ValueTask ConnectAsync()
    public void Dispose()
    public Task<Exception> DisconnectedAsync()

`ConnectAsync()` takes NO CancellationToken — there is no
`ConnectAsync(CancellationToken)` overload. On failure it throws
`ConnectException` (or `DisconnectedException` / `ObjectDisposedException`).
Unless `AutoConnect` is set, it may be called only once.
`DisconnectedAsync()` completes with the exception that ended the
connection.

Calling methods:

    public async Task CallMethodAsync(MessageBuffer message)
    public async Task<T> CallMethodAsync<T>(MessageBuffer message,
                                            MessageValueReader<T> reader,
                                            object readerState = null)

THE NON-GENERIC OVERLOAD RETURNS `Task` — IT GIVES YOU NO REPLY OBJECT. To
read a reply you must use the generic overload and supply a
`MessageValueReader<T>`; your delegate is called with the reply `Message`
and decodes it. If the peer answers with an error message, the returned Task
faults with `DBusException` (carrying `ErrorName` and `ErrorMessage`).

Fire-and-forget sending (signals, or calls flagged `NoReplyExpected`):

    public bool TrySendMessage(MessageBuffer message)

Returns false (and returns the buffer to the pool) when the connection is
not currently connected.

Subscribing to signals — four overloads; the first is obsolete:

    [Obsolete("Use an overload that accepts ObserverFlags.")]
    public ValueTask<IDisposable> AddMatchAsync<T>(
        MatchRule rule, MessageValueReader<T> reader,
        Action<Exception, T, object, object> handler,
        object readerState = null, object handlerState = null,
        bool emitOnCapturedContext = true, bool subscribe = true)

    public ValueTask<IDisposable> AddMatchAsync<T>(
        MatchRule rule, MessageValueReader<T> reader,
        Action<Exception, T, object, object> handler,
        ObserverFlags flags,
        object readerState = null, object handlerState = null,
        bool emitOnCapturedContext = true)

    public ValueTask<IDisposable> AddMatchAsync<T>(
        MatchRule rule, MessageValueReader<T> reader,
        Action<Exception, T, object, object> handler,
        object readerState, object handlerState,
        bool emitOnCapturedContext, ObserverFlags flags)

    public async ValueTask<IDisposable> AddMatchAsync<T>(
        MatchRule rule, MessageValueReader<T> reader,
        Action<Exception, T, object, object> handler,
        object readerState, object handlerState,
        SynchronizationContext synchronizationContext, ObserverFlags flags)

Prefer the second form: `AddMatchAsync(rule, reader, handler, flags)`.
Dispose the returned `IDisposable` to unsubscribe. The handler's four
arguments are `(exception, value, readerState, handlerState)`; `exception`
is null for a normal delivery and non-null for a terminal notification.
`emitOnCapturedContext: true` captures `SynchronizationContext.Current` at
subscribe time and delivers on it; `false` delivers on the connection's own
read loop.

Exposing objects (server side):

    public void AddMethodHandler(IMethodHandler methodHandler)
    public void AddMethodHandlers(IReadOnlyList<IMethodHandler> methodHandlers)
    public void RemoveMethodHandler(string path)
    public void RemoveMethodHandlers(IEnumerable<string> paths)

Creating messages:

    public MessageWriter GetMessageWriter()

Every outgoing message starts here. The writer is bound to the connection
that produced it (it carries that connection's next serial number), so never
send a buffer produced by one connection over another.

Bus utilities and monitoring:

    public const string DBusObjectPath  = "/org/freedesktop/DBus";
    public const string DBusServiceName = "org.freedesktop.DBus";
    public const string DBusInterface   = "org.freedesktop.DBus";

    public Task<string[]> ListServicesAsync()
    public Task<string[]> ListActivatableServicesAsync()
    public async Task BecomeMonitorAsync(
        Action<Exception, DisposableMessage> handler,
        IEnumerable<MatchRule> rules = null)
    public static async IAsyncEnumerable<DisposableMessage> MonitorBusAsync(
        string address, IEnumerable<MatchRule> rules = null,
        CancellationToken ct = default)

`BecomeMonitorAsync` throws `InvalidOperationException` on a shared
connection (`Connection.Session` / `Connection.System`). In a monitor
callback the message is handed to you inside a `DisposableMessage` — dispose
it when you are done with it.

There is NO `Connection.CreateMessage(...)` method, no `RequestNameAsync`
and no `ReleaseNameAsync` on `Connection`. Owning a bus name is done by
calling `RequestName` / `ReleaseName` on `org.freedesktop.DBus` yourself
(see COMPLETE EXAMPLES).


ADDRESS
-------
    public static class Address
    {
        public static string System  { get; }
        public static string Session { get; }
    }

`System` returns `$DBUS_SYSTEM_BUS_ADDRESS`, falling back to
`unix:path=/var/run/dbus/system_bus_socket` on non-Windows hosts. `Session`
returns `$DBUS_SESSION_BUS_ADDRESS`, falling back to the X11 selection
lookup (needs `$DISPLAY` and `libX11`). EITHER MAY RETURN NULL when the bus
cannot be located; `new Connection(null)` throws `ArgumentNullException`, so
check first. Both values are resolved once and cached for the life of the
process.


CONNECTION OPTIONS
------------------
    public abstract class ConnectionOptions          // internal ctor

    public class ClientConnectionOptions : ConnectionOptions
    {
        public ClientConnectionOptions(string address)
        protected internal ClientConnectionOptions()
        public bool AutoConnect { get; set; }
        protected internal virtual ValueTask<ClientSetupResult> SetupAsync(
            CancellationToken cancellationToken)
        protected internal virtual void Teardown(object token)
    }

With `AutoConnect = true` the first operation connects on demand and later
operations reconnect after a drop. The cost is that `UniqueName`,
`TrySendMessage`, `DisconnectedAsync`, `AddMethodHandler(s)` and
`RemoveMethodHandler(s)` all throw
`InvalidOperationException("Method cannot be used on autoconnect
connections.")` — they need a connection that is known to be established.
So: use AutoConnect for pure client call/subscribe code, and a plain
non-AutoConnect connection whenever you host objects or send raw messages.

Override `SetupAsync` to supply the address (or an already-connected
`Stream`) at connect time — that is how you connect over a socket you opened
yourself, or perform an out-of-band handshake first. `Teardown` is called
with `ClientSetupResult.TeardownToken` when the connection ends.

    public class ClientSetupResult
    {
        public ClientSetupResult(string address)
        public ClientSetupResult()
        public string ConnectionAddress  { get; }
        public object TeardownToken      { get; set; }
        public string UserId             { get; set; }
        public string MachineId          { get; set; }
        public bool   SupportsFdPassing  { get; set; }
        public Stream ConnectionStream    { get; set; }
    }

When `ConnectionStream` is set, `ConnectionAddress` and `SupportsFdPassing`
are ignored and the library uses your stream (and assumes it may be disposed
while reads/writes are in flight).


WRITING MESSAGES — MessageWriter AND MessageBuffer
--------------------------------------------------
    public ref partial struct MessageWriter

A `ref struct`: it lives on the stack only. It cannot be a field, cannot be
captured by a lambda, cannot cross an `await`, and cannot be used in an
`async` method body. Build the whole message inside one synchronous scope
(a local function is the idiomatic shape) and hand out the `MessageBuffer`.

    using var writer = connection.GetMessageWriter();
    ...
    MessageBuffer buffer = writer.CreateMessage();

    public MessageBuffer CreateMessage()
    public void Dispose()

`CreateMessage()` finalises lengths and transfers ownership of the buffer to
you; the writer is left empty, so the `using`'s `Dispose()` afterwards is
harmless and correct. If you abandon a writer WITHOUT calling
`CreateMessage()`, `Dispose()` returns the pooled buffer — always wrap the
writer in `using`.

HEADERS (write exactly one of these first, before any body value):

    public void WriteMethodCallHeader(
        string destination = null, string path = null,
        string @interface = null, string member = null,
        string signature = null,
        MessageFlags flags = MessageFlags.None)

    public void WriteMethodReturnHeader(
        uint replySerial, ReadOnlySpan<byte> destination = default,
        string signature = null)

    public void WriteError(
        uint replySerial, ReadOnlySpan<byte> destination = default,
        string errorName = null, string errorMsg = null)

    public void WriteSignalHeader(
        string destination = null, string path = null,
        string @interface = null, string member = null,
        string signature = null)

`signature` is the D-Bus signature of the BODY you are about to write —
"s" for one string, "ss" for two, "a{sv}" for a string-to-variant map, null
or omitted for an empty body.

BASIC VALUES:

    public void WriteByte(byte value)
    public void WriteBool(bool value)
    public void WriteInt16(short value)     public void WriteUInt16(ushort v)
    public void WriteInt32(int value)       public void WriteUInt32(uint v)
    public void WriteInt64(long value)      public void WriteUInt64(ulong v)
    public void WriteDouble(double value)
    public void WriteString(string value)
    public void WriteString(scoped ReadOnlySpan<byte> value)
    public void WriteObjectPath(string value)
    public void WriteObjectPath(ObjectPath value)
    public void WriteObjectPath(scoped ReadOnlySpan<byte> value)
    public void WriteSignature(string s)
    public void WriteSignature(Signature value)
    public void WriteSignature(scoped ReadOnlySpan<byte> value)

VALUES WRAPPED IN A VARIANT (signature byte then value, in one call):

    public void WriteVariantByte(byte value)
    public void WriteVariantBool(bool value)
    public void WriteVariantInt16(short value)
    public void WriteVariantUInt16(ushort value)
    public void WriteVariantInt32(int value)
    public void WriteVariantUInt32(uint value)
    public void WriteVariantInt64(long value)
    public void WriteVariantUInt64(ulong value)
    public void WriteVariantDouble(double value)
    public void WriteVariantString(string value)
    public void WriteVariantString(scoped ReadOnlySpan<byte> value)
    public void WriteVariantObjectPath(string value)
    public void WriteVariantObjectPath(scoped ReadOnlySpan<byte> value)
    public void WriteVariantSignature(string value)
    public void WriteVariantSignature(scoped ReadOnlySpan<byte> value)
    public void WriteVariantHandle(SafeHandle value)
    public void WriteVariant(VariantValue value)
    public void WriteVariant(Variant value)      // Variant is obsolete

ARRAYS — a whole-collection overload exists for every element type:

    public void WriteArray(byte[] value)
    public void WriteArray(ReadOnlySpan<byte> value)
    public void WriteArray(IEnumerable<byte> value)

...and the same three shapes for `short`, `ushort`, `int`, `uint`, `long`,
`ulong`, `double`, `string`, `Signature`, `ObjectPath`, `VariantValue`,
`Variant` and `SafeHandle` (42 overloads in total).

For an element type with no ready-made overload — an array of structs, an
array of arrays — build it by hand:

    public ArrayStart WriteArrayStart(DBusType elementType)
    public void WriteArrayEnd(ArrayStart start)
    public void WriteStructureStart()

    public ref struct ArrayStart      // opaque token, no public members

    ArrayStart start = writer.WriteArrayStart(DBusType.Struct);
    foreach (var item in items)
    {
        writer.WriteStructureStart();   // aligns to 8 before each field set
        writer.WriteString(item.Name);
        writer.WriteUInt32(item.Value);
    }
    writer.WriteArrayEnd(start);

`WriteStructureStart()` is also how you write a bare (non-array) struct: call
it once, then write the fields.

DICTIONARIES:

    public ArrayStart WriteDictionaryStart()
    public void WriteDictionaryEnd(ArrayStart start)
    public void WriteDictionaryEntryStart()

    public void WriteDictionary(Dictionary<string, VariantValue> value)
    public void WriteDictionary(KeyValuePair<string, VariantValue>[] value)
    public void WriteDictionary(
        IEnumerable<KeyValuePair<string, VariantValue>> value)
    public void WriteDictionary(Dictionary<string, Variant> value)
    public void WriteDictionary(KeyValuePair<string, Variant>[] value)
    public void WriteDictionary(
        IEnumerable<KeyValuePair<string, Variant>> value)

The ready-made overloads cover the very common `a{sv}` type only. Prefer the
`VariantValue` ones; the `Variant` ones are obsolete along with `Variant`
itself. Any other key/value combination is written manually with
`WriteDictionaryStart` / `WriteDictionaryEntryStart` / `WriteDictionaryEnd`.

UNIX FILE DESCRIPTORS:

    public void WriteHandle(SafeHandle value)
    public void WriteVariantHandle(SafeHandle value)

INTROSPECTION XML:

    public void WriteIntrospectionXml(
        scoped ReadOnlySpan<ReadOnlyMemory<byte>> interfaceXmls,
        IEnumerable<string> childNames)

MESSAGE BUFFER:

    public sealed class MessageBuffer

An opaque, pooled, ready-to-send message. It has NO public members: you
obtain one from `MessageWriter.CreateMessage()` and you pass it to
`Connection.CallMethodAsync`, `Connection.TrySendMessage` or
`MethodContext.Reply`. Each of those takes ownership and returns the buffer
to the pool — a buffer is single-use, and you must not send it twice.


READING MESSAGES — Message AND Reader
-------------------------------------
    public sealed class Message

The received message. Not `IDisposable` — you never dispose it and never
store it: it is valid only for the duration of the callback that received
it, after which it goes back to the pool.

    public bool        IsBigEndian  { get; }
    public uint        Serial       { get; }
    public MessageFlags MessageFlags{ get; }
    public MessageType MessageType  { get; }
    public uint?       ReplySerial  { get; }
    public int         UnixFdCount  { get; }

    public Reader GetBodyReader()

Header string fields come in three flavours — as a decoded string, as raw
UTF-8 bytes (allocation-free, ideal for comparisons), and as an "is it
present at all" flag:

    public string PathAsString        public ReadOnlySpan<byte> Path
    public string InterfaceAsString   public ReadOnlySpan<byte> Interface
    public string MemberAsString      public ReadOnlySpan<byte> Member
    public string ErrorNameAsString   public ReadOnlySpan<byte> ErrorName
    public string DestinationAsString public ReadOnlySpan<byte> Destination
    public string SenderAsString      public ReadOnlySpan<byte> Sender
    public string SignatureAsString   public ReadOnlySpan<byte> Signature

    public bool PathIsSet, InterfaceIsSet, MemberIsSet, ErrorNameIsSet,
                DestinationIsSet, SenderIsSet, SignatureIsSet

    public struct DisposableMessage : IDisposable
    {
        public Message Message { get; }
        public void Dispose()
    }

`DisposableMessage` is what monitor callbacks receive; disposing it releases
the message back to the pool.

    public ref partial struct Reader

Also a `ref struct` — same restrictions as `MessageWriter`. Obtain it with
`message.GetBodyReader()`, keep it in a local, and read the body fields in
the order the signature declares. There is no seeking and no "read field by
name".

BASIC READS:

    public byte   ReadByte()          public bool  ReadBool()
    public short  ReadInt16()         public ushort ReadUInt16()
    public int    ReadInt32()         public uint  ReadUInt32()
    public long   ReadInt64()         public ulong ReadUInt64()
    public double ReadDouble()
    public string ReadString()
    public ReadOnlySpan<byte> ReadStringAsSpan()
    public ObjectPath ReadObjectPath()
    public string ReadObjectPathAsString()
    public ReadOnlySpan<byte> ReadObjectPathAsSpan()
    public Signature ReadSignature()
    public string ReadSignatureAsString()
    public ReadOnlySpan<byte> ReadSignatureAsSpan()
    public void ReadSignature(string expected)
    public void ReadSignature(ReadOnlySpan<byte> expected)

The two `void ReadSignature(expected)` overloads read a signature and throw
`ProtocolException` when it is not the one you expected — a cheap way to
validate a variant's inner type before decoding it.

WHOLE-ARRAY READS:

    public byte[]        ReadArrayOfByte()
    public bool[]        ReadArrayOfBool()
    public short[]       ReadArrayOfInt16()
    public ushort[]      ReadArrayOfUInt16()
    public int[]         ReadArrayOfInt32()
    public uint[]        ReadArrayOfUInt32()
    public long[]        ReadArrayOfInt64()
    public ulong[]       ReadArrayOfUInt64()
    public double[]      ReadArrayOfDouble()
    public string[]      ReadArrayOfString()
    public ObjectPath[]  ReadArrayOfObjectPath()
    public Signature[]   ReadArrayOfSignature()
    public VariantValue[] ReadArrayOfVariantValue()
    public T[]           ReadArrayOfHandle<T>() where T : SafeHandle, new()

ELEMENT-BY-ELEMENT READS (arrays of structs, nested arrays):

    public ArrayEnd ReadArrayStart(DBusType elementType)
    public bool HasNext(ArrayEnd iterator)
    public void SkipTo(ArrayEnd end)
    public void AlignStruct()

    public ref struct ArrayEnd        // opaque token, no public members

`HasNext` advances to the next element boundary and reports whether one
exists; `SkipTo` jumps to the end of the array, discarding the remainder.
`AlignStruct()` aligns to 8 bytes and is what you call before reading a bare
struct that is not already an array element.

DICTIONARIES:

    public ArrayEnd ReadDictionaryStart()
    public Dictionary<string, VariantValue>
        ReadDictionaryOfStringToVariantValue()

The typed helper covers `a{sv}`. For any other key/value pair, use
`ReadDictionaryStart()` and then, per entry, read the key and then the
value.

VARIANTS AND FILE DESCRIPTORS:

    public VariantValue ReadVariantValue()
    public T ReadHandle<T>() where T : SafeHandle, new()
    public IntPtr ReadHandleRaw()

`ReadHandle<T>` takes ownership of the descriptor and gives you a
`SafeHandle` you must dispose; `ReadHandleRaw()` gives you the raw fd
without transferring ownership.

SIGNATURES:

    public ref struct SignatureReader
    {
        public SignatureReader(ReadOnlySpan<byte> signature)
        public ReadOnlySpan<byte> Signature { get; }
        public bool TryRead(out DBusType type,
                            out ReadOnlySpan<byte> innerSignature)
    }

Walks a signature one complete type at a time — use it to decide, at run
time, how to decode a body whose shape you did not know in advance.


SERVER SIDE — IMethodHandler AND MethodContext
----------------------------------------------
Implement `IMethodHandler` and register it with
`connection.AddMethodHandler(handler)` to answer incoming method calls on
one object path.

    public interface IMethodHandler
    {
        string Path { get; }
        ValueTask HandleMethodAsync(MethodContext context);
        bool RunMethodHandlerSynchronously(Message message);
    }

  - `Path` is the object path this handler owns, e.g. "/com/example/Object".
    It must start with '/' and contain no empty segment ("//"), otherwise
    `AddMethodHandler` throws `FormatException`. ONE HANDLER PER PATH:
    registering a second handler for a path that already has one throws
    `InvalidOperationException` — remove the first with
    `RemoveMethodHandler(path)` if you mean to replace it.
  - `HandleMethodAsync` is called for every method call on that path.
    `context.Request` is valid ONLY for the duration of the call — never
    store it.
  - `RunMethodHandlerSynchronously(message)` is asked before every dispatch.
    Return `true` (the usual answer) and the connection awaits your handler
    before reading the next message — simple, ordered, and it blocks the
    read loop. Return `false` and the handler runs detached, so the loop
    keeps reading; you then handle concurrency yourself.

If your handler returns without sending a reply, the library replies for
you: `org.freedesktop.DBus.Peer.Ping` and `.GetMachineId` are answered
automatically, an introspection request gets the generated XML, and anything
else gets `org.freedesktop.DBus.Error.UnknownMethod`. So an unhandled member
does NOT hang the caller.

    public class MethodContext
    {
        public Message Request                 { get; }
        public Connection Connection           { get; }
        public CancellationToken RequestAborted{ get; }
        public bool ReplySent                  { get; }
        public bool NoReplyExpected            { get; }
        public bool IsDBusIntrospectRequest    { get; }

        public MessageWriter CreateReplyWriter(string signature)
        public void Reply(MessageBuffer message)
        public void ReplyError(string errorName = null,
                               string errorMsg = null)
        public void ReplyIntrospectXml(
            ReadOnlySpan<ReadOnlyMemory<byte>> interfaceXmls)
    }

  - `CreateReplyWriter(signature)` returns a `MessageWriter` already carrying
    the method-return header (reply serial and destination filled in from the
    request); `signature` describes the reply body, or null for an empty one.
  - `Reply(...)` sends it, and sets `ReplySent`. Calling `Reply` a second
    time throws `InvalidOperationException`. CHECK `NoReplyExpected` FIRST
    AND DO NOT BUILD OR SEND A REPLY WHEN IT IS TRUE — the caller asked not
    to receive one, and the library does not send an automatic reply in that
    case either.
  - `ReplyError(errorName, errorMsg)` sends a D-Bus error; the caller's
    `CallMethodAsync` then throws `DBusException` with those values.
  - `RequestAborted` is cancelled when the connection goes away, which is how
    a long-running handler learns to stop.
  - `ReplyIntrospectXml(...)` may be called only while
    `IsDBusIntrospectRequest` is true, otherwise it throws
    `InvalidOperationException`. Pass the interface XML blocks for this
    object; the standard `Introspectable` and `Peer` interfaces and the child
    node names are added for you.

Ready-made interface XML blocks for the standard interfaces:

    public static class IntrospectionXml
    {
        public static ReadOnlyMemory<byte> DBusProperties     { get; }
        public static ReadOnlyMemory<byte> DBusIntrospectable { get; }
        public static ReadOnlyMemory<byte> DBusPeer           { get; }
    }


VARIANTS — VariantValue (USE THIS) AND Variant (OBSOLETE)
---------------------------------------------------------
    public readonly struct VariantValue : IEquatable<VariantValue>

The owned, readable representation of a D-Bus variant. This is the type to
use for both reading and writing.

    public VariantValueType Type      { get; }
    public VariantValueType ItemType  { get; }   // arrays
    public VariantValueType KeyType   { get; }   // dictionaries
    public VariantValueType ValueType { get; }   // dictionaries
    public int Count { get; }   // arrays/structs/dictionaries; -1 otherwise

TYPED GETTERS — each throws when `Type` does not match:

    public byte   GetByte()        public bool   GetBool()
    public short  GetInt16()       public ushort GetUInt16()
    public int    GetInt32()       public uint   GetUInt32()
    public long   GetInt64()       public ulong  GetUInt64()
    public double GetDouble()      public string GetString()
    public string GetObjectPathAsString()
    public Signature GetSignature()
    public VariantValue GetVariantValue()          // unwraps a nested variant
    public T ReadHandle<T>() where T : SafeHandle, new()

    [Obsolete] public string GetObjectPath()       // use GetObjectPathAsString

CONTAINER ACCESS:

    public VariantValue GetItem(int i)                       // array, struct
    public KeyValuePair<VariantValue, VariantValue> GetDictionaryEntry(int i)
    public T[] GetArray<T>()                                 // typed copy
    public Dictionary<TKey, TValue> GetDictionary<TKey, TValue>()

CONSTRUCTION — implicit conversions exist for `byte`, `bool`, `short`,
`ushort`, `int`, `uint`, `long`, `ulong`, `double`, `string`, `ObjectPath`
and `Signature`; the named factories are explicit about the wire type:

    public static VariantValue Byte(byte value)
    public static VariantValue Bool(bool value)
    public static VariantValue Int16(short value)
    public static VariantValue UInt16(ushort value)
    public static VariantValue Int32(int value)
    public static VariantValue UInt32(uint value)
    public static VariantValue Int64(long value)
    public static VariantValue UInt64(ulong value)
    public static VariantValue Double(double value)
    public static VariantValue String(string value)
    public static VariantValue ObjectPath(ObjectPath value)
    public static VariantValue Signature(Signature value)
    public static VariantValue UnixFd(SafeHandle handle)
    public static VariantValue Variant(VariantValue value)   // nests one level

    public static VariantValue Struct(VariantValue item1)
    ... up to ...
    public static VariantValue Struct(VariantValue item1, ...,
                                      VariantValue item10)

    public static VariantValue Array(byte[] items)      // and List<byte>
    ... the same pair for short, ushort, int, uint, long, ulong, double,
        bool, string, ObjectPath, Signature and SafeHandle ...
    public static VariantValue ArrayOfVariant(VariantValue[] items)
    public static VariantValue ArrayOfVariant(List<VariantValue> items)

There is NO public `VariantValue.Dictionary(...)` factory. To WRITE a
dictionary use `MessageWriter.WriteDictionary(...)` or `Dict<TKey,TValue>`;
`VariantValue` reads dictionaries but does not construct them.

    public string ToString()                    // includes a type suffix
    public string ToString(bool includeTypeSuffix)
    public bool Equals(VariantValue other)
    public static bool operator==(VariantValue lhs, VariantValue rhs)
    public static bool operator!=(VariantValue lhs, VariantValue rhs)

`Variant` is the older, write-only, lightweight variant view:

    [Obsolete] public readonly struct Variant

It is marked `[Obsolete]` in Release builds ("Variant will be removed. Use
the VariantValue type instead."), so consuming it produces CS0618 in your
Release build. It has constructors and implicit conversions for every basic
type, plus `Variant.FromArray<T>(Array<T>)`,
`Variant.FromDict<TKey,TValue>(Dict<TKey,TValue>)` and
`Variant.FromStruct(Struct<...>)` for one through ten fields. The extension
class

    public static class VariantExtensions
    {
        public static Variant AsVariant(this byte value)
        ... bool, short, ushort, int, uint, long, ulong, double, string,
            SafeHandle ...
    }

and the `AsVariant()` methods on `ObjectPath`, `Signature`, `Array<T>`,
`Dict<,>` and `Struct<...>` all produce this obsolete type. NEW CODE SHOULD
USE `VariantValue` AND `AsVariantValue()` INSTEAD.


TYPED CONTAINER HELPERS — Array<T>, Dict<K,V>, Struct<...>
----------------------------------------------------------
These wrap ordinary collections so they can be handed to the writer or
converted to a `VariantValue` without hand-rolling the wire layout.

    public sealed class Array<T> : IDBusWritable, IList<T>,
                                   IVariantValueConvertable
        where T : notnull
    {
        public Array()
        public Array(int capacity)
        public Array(IEnumerable<T> collection)
        public int Count { get; }
        public T this[int index] { get; set; }
        public void Add(T item)          public void Clear()
        public void Insert(int index, T item)
        public void RemoveAt(int index)  public bool Remove(T item)
        public int IndexOf(T item)       public bool Contains(T item)
        public void CopyTo(T[] array, int arrayIndex)
        public VariantValue AsVariantValue()
        public Variant AsVariant()                       // obsolete type
        public static implicit operator VariantValue(Array<T> value)
    }

CALLING `AsVariantValue()` FREEZES THE ARRAY: every later mutation throws
`InvalidOperationException`. Build it fully, then convert.

    public sealed class Dict<TKey, TValue> : IDBusWritable,
        IDictionary<TKey, TValue>, IVariantValueConvertable
        where TKey : notnull where TValue : notnull
    {
        public Dict()
        public Dict(IDictionary<TKey, TValue> dictionary)
        public int Count { get; }
        public TValue this[TKey key] { get; set; }
        public void Add(TKey key, TValue value)
        public bool ContainsKey(TKey key)
        public bool Remove(TKey key)
        public bool TryGetValue(TKey key, out TValue value)
        public void Clear()
        public VariantValue AsVariantValue()
        public Variant AsVariant()                       // obsolete type
    }

    public static class Struct
    {
        public static Struct<T1> Create<T1>(T1 item1)
        public static Struct<T1,T2> Create<T1,T2>(T1 item1, T2 item2)
        ... through ...
        public static Struct<T1,...,T10> Create<T1,...,T10>(...)
    }

    public sealed class Struct<T1> : IDBusWritable, IVariantValueConvertable
    {
        public T1 Item1;                       // public FIELD, not property
        public Struct(T1 item1)
        public VariantValue AsVariantValue()
        public Variant AsVariant()                       // obsolete type
        public static implicit operator VariantValue(Struct<T1> value)
    }

...and likewise `Struct<T1,T2>` through `Struct<T1,...,T10>`, each with
`Item1`..`ItemN` fields. The element/field types are checked at construction
time: an unsupported type throws immediately, not at write time.

    public interface IVariantValueConvertable
    {
        VariantValue AsVariantValue();
    }

    public interface IDBusWritable
    {
        void WriteTo(ref MessageWriter writer);
    }

`IDBusWritable` is implemented EXPLICITLY by `Array<T>`, `Dict<,>` and
`Struct<...>` — cast to the interface if you want to call `WriteTo`
directly. Implement it on your own type to make it writable the same way.


WIRE-LEVEL VALUE TYPES
----------------------
    public readonly struct ObjectPath
    {
        public ObjectPath(string value)          // throws on empty
        public override string ToString()
        public static implicit operator string(ObjectPath value)
        public static implicit operator ObjectPath(string value)
        public Variant AsVariant()               // obsolete type
    }

    public readonly struct Signature
    {
        [Obsolete] public Signature(string value)
        public Signature(ReadOnlySpan<byte> value)
        public override string ToString()
        public static implicit operator Signature(ReadOnlySpan<byte> value)
        public Variant AsVariant()               // obsolete type
    }

The `Signature(string)` constructor is obsolete — build signatures from
UTF-8 literals instead: `new Signature("a{sv}"u8)`. Note that the header
writers take signatures as plain `string`, so this only matters when you
materialise a `Signature` value yourself.


ENUMERATIONS
------------
    public enum MessageType : byte
    { MethodCall = 1, MethodReturn = 2, Error = 3, Signal = 4 }

    [Flags] public enum MessageFlags : byte
    { None = 0, NoReplyExpected = 1, NoAutoStart = 2,
      AllowInteractiveAuthorization = 4 }

    public enum DBusType : byte
    { Invalid = 0, Byte = 'y', Bool = 'b', Int16 = 'n', UInt16 = 'q',
      Int32 = 'i', UInt32 = 'u', Int64 = 'x', UInt64 = 't', Double = 'd',
      String = 's', ObjectPath = 'o', Signature = 'g', Array = 'a',
      Struct = '(', Variant = 'v', DictEntry = '{', UnixFd = 'h' }

    public enum VariantValueType : byte
    { Invalid = 0, Byte, Bool, Int16, UInt16, Int32, UInt32, Int64, UInt64,
      Double, String, ObjectPath, Signature, Array, Struct, Dictionary,
      UnixFd, Variant }        // members share DBusType's character values,
                               // except Dictionary, which is DictEntry ('{')

    [Flags] public enum ObserverFlags
    {
        None                    = 0,
        EmitOnConnectionDispose = 1,
        EmitOnObserverDispose   = 2,
        NoSubscribe             = 4,
        EmitOnDispose           = EmitOnConnectionDispose |
                                  EmitOnObserverDispose
    }

`ObserverFlags` controls what a signal subscription does at its edges:
`EmitOnConnectionDispose` calls your handler with an exception when the
connection is disposed, `EmitOnObserverDispose` does the same when you
dispose the subscription, and `NoSubscribe` registers the match locally
without sending `AddMatch` to the bus (use it when another subscription
already covers the traffic). `EmitOnDispose` is the both-edges combination.
When the peer is not a bus, `NoSubscribe` is applied automatically.


MATCH RULES
-----------
    public sealed class MatchRule
    {
        public MessageType? Type   { get; set; }
        public string Sender       { get; set; }
        public string Interface    { get; set; }
        public string Member       { get; set; }
        public string Path         { get; set; }
        public string PathNamespace{ get; set; }
        public string Destination  { get; set; }
        public string Arg0         { get; set; }
        public string Arg0Path     { get; set; }
        public string Arg0Namespace{ get; set; }
        public override string ToString()   // the D-Bus match-rule string
    }

Every property is optional; the ones you set are ANDed together. Per the
D-Bus specification `Path` and `PathNamespace` may not both be used on one
rule, and neither may `Arg0`, `Arg0Path` and `Arg0Namespace` — the class
does not enforce that, so do not set both.

`ToString()` returns the rule string exactly as it is sent to
`org.freedesktop.DBus.AddMatch`, which makes it easy to log or to compare
with `dbus-monitor`. Every key is emitted with the spelling the D-Bus
specification defines, including `path_namespace`, `arg0path` and
`arg0namespace`.


DELEGATES
---------
    public delegate T MessageValueReader<T>(Message message, object state);

The only public delegate type in the package. It decodes a received message
into a value of your choice, and it is used by both `CallMethodAsync<T>` and
`AddMatchAsync<T>`. `state` is the `readerState` you passed in. Because
`Reader` is a `ref struct`, ALL decoding must happen inside this delegate —
you cannot return a `Reader` from it or capture one outside it.

Note: the source also declares a `MessageReceivedHandler<T>` delegate, but
it is nested inside an internal interface and is therefore NOT accessible to
consumers. The same is true of `UnixFdCollection`, which is internal — Unix
descriptors reach you as `SafeHandle` values through `Reader.ReadHandle<T>`,
`Reader.ReadArrayOfHandle<T>` and `VariantValue.ReadHandle<T>`.


EXCEPTIONS
----------
    public class DBusException : Exception
    {
        public DBusException(string errorName, string errorMessage)
        public string ErrorName    { get; }
        public string ErrorMessage { get; }
    }

Thrown by `CallMethodAsync` when the peer replies with a D-Bus error.
`ErrorName` is the D-Bus error name, e.g.
"org.freedesktop.DBus.Error.UnknownMethod".

    public class ConnectException : Exception
    {
        public ConnectException(string message)
        public ConnectException(string message, Exception innerException)
    }

    public class DisconnectedException : Exception   // internal constructor

Thrown when the connection is closed — including for calls that were still
in flight. Its `InnerException` is the reason.

    public class ProtocolException : Exception
    {
        public ProtocolException(string message)
    }

Malformed or unexpected wire data (a bad signature, a truncated body).

    public static class ActionException
    {
        public static bool IsObserverDisposed(Exception exception)
        public static bool IsConnectionDisposed(Exception exception)
        public static bool IsDisposed(Exception exception)
    }

Use these inside a signal handler to tell an orderly shutdown ("you disposed
the subscription", "you disposed the connection") from a real failure,
instead of matching on exception types or messages.


COMPLETE EXAMPLES
=================

1 — CALL A METHOD AND READ THE REPLY (SESSION BUS)
--------------------------------------------------
    using System;
    using System.Threading.Tasks;
    using CodeBrix.Platform.LinuxDBus;

    public static class ListNamesExample
    {
        public static async Task RunAsync()
        {
            string address = Address.Session;
            if (address == null)
            {
                Console.WriteLine("No session bus available.");
                return;
            }

            using var connection = new Connection(address);
            await connection.ConnectAsync();

            string[] names = await connection.CallMethodAsync(
                CreateMessage(connection),
                (Message message, object state) =>
                    message.GetBodyReader().ReadArrayOfString());

            foreach (string name in names)
            {
                Console.WriteLine(name);
            }
        }

        // A local/static helper keeps the ref struct writer out of the
        // async method body.
        private static MessageBuffer CreateMessage(Connection connection)
        {
            using var writer = connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Connection.DBusServiceName,
                path:        Connection.DBusObjectPath,
                @interface:  Connection.DBusInterface,
                member:      "ListNames");
            return writer.CreateMessage();
        }
    }

A call WITH arguments differs only in the header's `signature` plus the body
writes that follow it:

    private static MessageBuffer CreateConcatMessage(
        Connection connection, string lhs, string rhs)
    {
        using var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: "com.example.Service",
            path:        "/com/example/Object",
            @interface:  "com.example.StringOperations",
            member:      "Concat",
            signature:   "ss");
        writer.WriteString(lhs);
        writer.WriteString(rhs);
        return writer.CreateMessage();
    }

    string joined = await connection.CallMethodAsync(
        CreateConcatMessage(connection, "hello ", "world"),
        (Message m, object s) => m.GetBodyReader().ReadString());


2 — SUBSCRIBE TO A SIGNAL
-------------------------
    using System;
    using System.Threading.Tasks;
    using CodeBrix.Platform.LinuxDBus;

    public static class SignalExample
    {
        public static async Task<IDisposable> WatchNameOwnerChangedAsync(
            Connection connection)
        {
            var rule = new MatchRule
            {
                Type      = MessageType.Signal,
                Sender    = Connection.DBusServiceName,
                Path      = Connection.DBusObjectPath,
                Interface = Connection.DBusInterface,
                Member    = "NameOwnerChanged",
            };

            return await connection.AddMatchAsync(
                rule,
                // reader: decode the "sss" body into a tuple
                (Message message, object state) =>
                {
                    Reader reader = message.GetBodyReader();
                    string name     = reader.ReadString();
                    string oldOwner = reader.ReadString();
                    string newOwner = reader.ReadString();
                    return (name, oldOwner, newOwner);
                },
                // handler: (exception, value, readerState, handlerState)
                (Exception exception,
                 (string Name, string Old, string New) value,
                 object readerState,
                 object handlerState) =>
                {
                    if (exception != null)
                    {
                        if (!ActionException.IsDisposed(exception))
                        {
                            Console.WriteLine(
                                $"subscription ended: {exception.Message}");
                        }
                        return;
                    }

                    Console.WriteLine(
                        $"{value.Name}: '{value.Old}' -> '{value.New}'");
                },
                ObserverFlags.EmitOnDispose);
        }
    }

    // elsewhere:
    IDisposable subscription =
        await SignalExample.WatchNameOwnerChangedAsync(connection);
    // ... later ...
    subscription.Dispose();          // unsubscribes

To deliver on the current SynchronizationContext (a UI thread, say), use the
longer overload and pass `emitOnCapturedContext: true`:

    await connection.AddMatchAsync(rule, reader, handler,
        readerState: null, handlerState: null,
        emitOnCapturedContext: true, flags: ObserverFlags.EmitOnDispose);


3 — EXPOSE AN OBJECT ON THE BUS WITH A METHOD HANDLER
-----------------------------------------------------
    using System;
    using System.Threading.Tasks;
    using CodeBrix.Platform.LinuxDBus;

    public sealed class StringOperations : IMethodHandler
    {
        public const string InterfaceName = "com.example.StringOperations";

        public string Path => "/com/example/StringOperations";

        // true: the connection awaits this handler before reading the next
        // message. Return false to let the read loop continue while a slow
        // handler runs.
        public bool RunMethodHandlerSynchronously(Message message) => true;

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            Message request = context.Request;

            if (request.InterfaceAsString == InterfaceName &&
                request.MemberAsString == "Concat" &&
                request.SignatureAsString == "ss")
            {
                return ConcatAsync(context);
            }

            // Returning without replying makes the library answer with
            // org.freedesktop.DBus.Error.UnknownMethod.
            return default;
        }

        private ValueTask ConcatAsync(MethodContext context)
        {
            Reader reader = context.Request.GetBodyReader();
            string lhs = reader.ReadString();
            string rhs = reader.ReadString();

            using var writer = context.CreateReplyWriter("s");
            writer.WriteString(lhs + rhs);
            context.Reply(writer.CreateMessage());

            return default;
        }
    }

Hosting it, including taking a well-known bus name (there is no
`RequestNameAsync` helper on `Connection`; call the bus method yourself):

    using var connection = new Connection(Address.Session);
    await connection.ConnectAsync();

    connection.AddMethodHandler(new StringOperations());

    await connection.CallMethodAsync(CreateRequestName(
        connection, "com.example.Service"));

    // keep the process alive until the bus drops us
    Exception reason = await connection.DisconnectedAsync();

    static MessageBuffer CreateRequestName(Connection connection,
                                           string name)
    {
        using var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: Connection.DBusServiceName,
            path:        Connection.DBusObjectPath,
            @interface:  Connection.DBusInterface,
            member:      "RequestName",
            signature:   "su");
        writer.WriteString(name);
        writer.WriteUInt32(0);      // flags: none
        return writer.CreateMessage();
    }

Reporting a failure back to the caller — the caller's `CallMethodAsync`
throws `DBusException` with exactly these values:

    context.ReplyError("com.example.Error.InvalidArgument",
                       "count must be greater than zero");


4 — SEND AND RECEIVE VARIANTS AND DICTIONARIES
----------------------------------------------
Writing an `a{sv}` body:

    private static MessageBuffer CreateSetPropertiesMessage(
        Connection connection)
    {
        var properties = new Dictionary<string, VariantValue>
        {
            ["Name"]    = VariantValue.String("example"),
            ["Count"]   = VariantValue.Int32(3),
            ["Enabled"] = VariantValue.Bool(true),
            ["Tags"]    = VariantValue.Array(
                              new[] { "alpha", "beta" }),
        };

        using var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: "com.example.Service",
            path:        "/com/example/Object",
            @interface:  "com.example.Config",
            member:      "Apply",
            signature:   "a{sv}");
        writer.WriteDictionary(properties);
        return writer.CreateMessage();
    }

Reading an `a{sv}` body and switching on the variant type:

    Dictionary<string, VariantValue> result =
        await connection.CallMethodAsync(
            CreateGetAllMessage(connection),
            (Message m, object s) =>
                m.GetBodyReader().ReadDictionaryOfStringToVariantValue());

    foreach (KeyValuePair<string, VariantValue> entry in result)
    {
        VariantValue value = entry.Value;
        switch (value.Type)
        {
            case VariantValueType.String:
                Console.WriteLine($"{entry.Key} = {value.GetString()}");
                break;
            case VariantValueType.UInt32:
                Console.WriteLine($"{entry.Key} = {value.GetUInt32()}");
                break;
            case VariantValueType.Bool:
                Console.WriteLine($"{entry.Key} = {value.GetBool()}");
                break;
            case VariantValueType.Array
                    when value.ItemType == VariantValueType.String:
                Console.WriteLine(
                    $"{entry.Key} = " +
                    $"[{string.Join(", ", value.GetArray<string>())}]");
                break;
            default:
                // ToString() renders the value with a type suffix
                Console.WriteLine($"{entry.Key} = {value}");
                break;
        }
    }

Writing a variant on its own (signature "v"), and reading one back:

    writer.WriteMethodCallHeader(..., signature: "v");
    writer.WriteVariant(VariantValue.String("example"));

    // reading:
    VariantValue v = message.GetBodyReader().ReadVariantValue();
    string text = v.GetString();


5 — SYSTEM BUS: READ A PROPERTY WITH org.freedesktop.DBus.Properties.Get
------------------------------------------------------------------------
    using System;
    using System.Threading.Tasks;
    using CodeBrix.Platform.LinuxDBus;

    public static class PropertyExample
    {
        // Returns e.g. the hostname from systemd-hostnamed.
        public static async Task<string> GetHostnameAsync()
        {
            using var connection = new Connection(Address.System);
            await connection.ConnectAsync();

            VariantValue value = await connection.CallMethodAsync(
                CreateGetMessage(connection,
                    destination: "org.freedesktop.hostname1",
                    path:        "/org/freedesktop/hostname1",
                    @interface:  "org.freedesktop.hostname1",
                    property:    "Hostname"),
                (Message m, object s) =>
                    m.GetBodyReader().ReadVariantValue());

            return value.GetString();
        }

        private static MessageBuffer CreateGetMessage(
            Connection connection, string destination, string path,
            string @interface, string property)
        {
            using var writer = connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: destination,
                path:        path,
                @interface:  "org.freedesktop.DBus.Properties",
                member:      "Get",
                signature:   "ss");
            writer.WriteString(@interface);   // interface owning the property
            writer.WriteString(property);     // property name
            return writer.CreateMessage();
        }
    }

`Properties.Get` returns a single variant ("v"), so the reply is decoded
with one `ReadVariantValue()`. `Properties.GetAll` returns "a{sv}" and is
decoded with `ReadDictionaryOfStringToVariantValue()` as in example 4.


6 — READ AN ARRAY OF STRUCTS ELEMENT BY ELEMENT
-----------------------------------------------
For a body with signature "a(sou)" — an array of (string, object path,
uint32) structs:

    (string Name, string Path, uint Id)[] ReadEntries(Message message)
    {
        var entries = new List<(string, string, uint)>();
        Reader reader = message.GetBodyReader();

        ArrayEnd end = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(end))
        {
            string name = reader.ReadString();
            string path = reader.ReadObjectPathAsString();
            uint   id   = reader.ReadUInt32();
            entries.Add((name, path, id));
        }

        return entries.ToArray();
    }

`HasNext` performs the per-element alignment, so no `AlignStruct()` call is
needed inside an array of structs. Use `AlignStruct()` when a struct appears
outside an array, before reading its first field. `SkipTo(end)` abandons the
remaining elements.

The matching write:

    writer.WriteMethodCallHeader(..., signature: "a(sou)");
    ArrayStart start = writer.WriteArrayStart(DBusType.Struct);
    foreach (var entry in entries)
    {
        writer.WriteStructureStart();
        writer.WriteString(entry.Name);
        writer.WriteObjectPath(entry.Path);
        writer.WriteUInt32(entry.Id);
    }
    writer.WriteArrayEnd(start);


MINIMUM VIABLE PROJECT
======================

MyDBusApp.csproj:

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>disable</Nullable>
      </PropertyGroup>
      <ItemGroup>
        <!-- add with:  dotnet add package
             CodeBrix.Platform.LinuxDBus.MitLicenseForever
             which fills in the Version attribute for you -->
        <PackageReference
            Include="CodeBrix.Platform.LinuxDBus.MitLicenseForever" />
      </ItemGroup>
    </Project>

Program.cs:

    using System;
    using System.Threading.Tasks;
    using CodeBrix.Platform.LinuxDBus;

    string address = Address.Session;
    if (address == null)
    {
        Console.Error.WriteLine("No session bus (DBUS_SESSION_BUS_ADDRESS).");
        return 1;
    }

    using var connection = new Connection(address);
    await connection.ConnectAsync();

    Console.WriteLine($"connected as {connection.UniqueName}");

    foreach (string name in await connection.ListServicesAsync())
    {
        Console.WriteLine(name);
    }

    return 0;

Run it on a Linux desktop session (or under `dbus-run-session -- dotnet
run`) and it prints every name currently on the session bus.


PERFORMANCE TIPS
================

  - Messages and their buffers are POOLED. `MessageWriter.CreateMessage()`
    hands the pooled buffer to the send path, and received `Message` objects
    return to the pool when your callback ends. That is the whole reason for
    the "do not store a Message" and "always `using` a writer" rules — break
    them and you either leak pooled objects or read recycled memory.

  - Read header fields as SPANS when you are only comparing them.
    `message.Interface.SequenceEqual("com.example.Foo"u8)` allocates
    nothing; `message.InterfaceAsString == "com.example.Foo"` allocates a
    string on every message. In a dispatch loop that runs per message, the
    difference is real.

  - Prefer the bulk array reads and writes (`ReadArrayOfInt32`,
    `WriteArray(ReadOnlySpan<int>)`) over element-by-element loops. For the
    fixed-width numeric types the bulk paths copy memory in one go.

  - Pass state through `readerState` / `handlerState` instead of capturing
    it in a lambda. The delegates are then cacheable static lambdas, and no
    closure is allocated per call or per signal.

  - Set `emitOnCapturedContext: false` on subscriptions that do not need a
    particular thread. Delivering on a captured SynchronizationContext uses
    a blocking `Send`, which stalls the connection's read loop until your
    context runs the callback.

  - Return `false` from `RunMethodHandlerSynchronously` for handlers that
    await I/O. Returning `true` keeps the read loop parked on your handler,
    so one slow method call delays every other message on the connection.

  - Use `Connection.Session` / `Connection.System` when many components in
    one process each want a bus connection; they are shared and
    auto-connecting, so you get one socket instead of many. (Mind their
    AutoConnect restrictions, below.)

  - Reuse one `Connection` for the lifetime of your component. Connecting
    performs a SASL handshake and a `Hello` round-trip; it is not something
    to do per call.


COMMON PITFALLS TO AVOID
========================

  - `CallMethodAsync(MessageBuffer)` RETURNS `Task`, NOT A REPLY. There is
    no reply object to inspect and nothing to `using`. To read a reply you
    must call `CallMethodAsync<T>(MessageBuffer, MessageValueReader<T>,
    object readerState = null)`.

  - `Connection` IS `IDisposable`, NOT `IAsyncDisposable`. `await using var
    connection = new Connection(...)` does not compile, and there is no
    `DisposeAsync()`. Use `using var` / `Dispose()`.

  - There is no `Connection.CreateMessage(...)`. Messages are built with
    `connection.GetMessageWriter()` plus a header-writing call.

  - `Reader` AND `MessageWriter` ARE `ref struct`s. They cannot be stored in
    a field, captured by a lambda, boxed, used across an `await`, or
    declared in an `async` method. Put message construction and message
    decoding in ordinary (non-async) local functions or methods — the
    `MessageValueReader<T>` delegate exists precisely so that decoding
    happens in a non-async scope.

  - `Message` IS NOT `IDisposable` AND MUST NOT BE STORED. It is valid only
    inside the callback that received it; afterwards it returns to the pool
    and its contents are recycled. Copy out the values you need.

  - A `MessageBuffer` IS SINGLE-USE. Sending it transfers ownership; do not
    send the same buffer twice, and do not send a buffer created from one
    connection's writer over a different connection (the serial number
    belongs to the connection that issued the writer).

  - ALWAYS WRAP THE WRITER IN `using`. If you build a message and then throw
    (or decide not to send), the pooled buffer is only reclaimed by
    `Dispose()`. After a successful `CreateMessage()` the extra `Dispose()`
    is a no-op, so `using` is always right.

  - THE SIGNATURE MUST MATCH THE BODY. The header's `signature` argument is
    not validated against what you subsequently write. A mismatch produces a
    message that the peer rejects, or worse, silently misreads.

  - A THROW INSIDE YOUR CALLBACK TEARS DOWN THE CONNECTION. That applies to
    the `MessageValueReader<T>`, to the signal handler and to
    `IMethodHandler.HandleMethodAsync`. The connection is disconnected with
    your exception as the reason, and the handler is then called once more
    with a `DisconnectedException` wrapping it. Guard your callbacks.

  - AUTOCONNECT CONNECTIONS REFUSE SEVERAL MEMBERS. On a connection created
    with `AutoConnect = true` — which includes the shared
    `Connection.Session` and `Connection.System` — `UniqueName`,
    `TrySendMessage`, `DisconnectedAsync`, `AddMethodHandler(s)` and
    `RemoveMethodHandler(s)` throw `InvalidOperationException("Method cannot
    be used on autoconnect connections.")`. Host objects on a plain
    connection you connected explicitly. `BecomeMonitorAsync` additionally
    refuses shared connections.

  - `ConnectAsync()` MAY BE CALLED ONLY ONCE unless `AutoConnect` is set;
    a second call throws `InvalidOperationException`. It takes no
    CancellationToken.

  - `Address.Session` / `Address.System` CAN RETURN NULL, and
    `new Connection(null)` throws `ArgumentNullException`. Check before
    constructing. Both are cached on first access, so setting
    `DBUS_SESSION_BUS_ADDRESS` after the first read has no effect.

  - A `MatchRule` WITH NO `Sender` ACCEPTS SIGNALS FROM EVERY PEER. Sender
    verification protects you only for the name you actually specify.

  - `Array<T>.AsVariantValue()` FREEZES THE ARRAY — later `Add`/`Clear`/
    indexer-set calls throw `InvalidOperationException`.

  - `Variant` IS OBSOLETE IN RELEASE BUILDS. `Variant`, the
    `VariantExtensions.AsVariant()` extensions, and every `AsVariant()`
    member produce CS0618 outside DEBUG. Use `VariantValue` and
    `AsVariantValue()`. Likewise `Signature(string)` is obsolete (use the
    `ReadOnlySpan<byte>` constructor with a `"..."u8` literal) and
    `VariantValue.GetObjectPath()` is obsolete (use
    `GetObjectPathAsString()`).

  - VariantValue GETTERS ARE TYPE-CHECKED AND THROW. Check `Type` (and
    `ItemType` / `KeyType` / `ValueType` for containers) before calling
    `GetString()`, `GetInt32()`, `GetArray<T>()` and friends.

  - AT MOST 16 UNIX FILE DESCRIPTORS PER MESSAGE, and only when the
    transport supports fd passing. `SafeHandle`s you receive are yours to
    dispose.

  - LINUX ONLY IN PRACTICE. The package will compile and load anywhere, but
    it needs a D-Bus daemon and it P/Invokes `libc` (and `libX11` when
    resolving the session address without an environment variable).

  - `AddMethodHandler` DOES NOT REPLACE AN EXISTING HANDLER — it throws
    `InvalidOperationException` when the path is already registered. Call
    `RemoveMethodHandler(path)` first. A path that does not start with '/',
    or that contains "//", throws `FormatException`.

  - MATCH-RULE KEYS ARE EMITTED PER SPECIFICATION. `MatchRule.ToString()` —
    the exact string sent to `org.freedesktop.DBus.AddMatch` — writes
    `path_namespace`, `arg0path` and `arg0namespace` (the D-Bus key
    spellings), so a bus accepts rules that use those three fields. If you
    build a rule string by hand instead of through `MatchRule`, use the same
    spellings; a bus rejects `pathNamespace`-style keys with `DBusException`.

  - DO NOT ALSO REFERENCE THE UPSTREAM PACKAGE. `Tmds.DBus.Protocol` and
    this package define the same type names; referencing both gives you
    ambiguous-reference build errors.


WHAT THIS PACKAGE DOES NOT DO
=============================

  - NO PROXY GENERATION AND NO HIGH-LEVEL CLIENT. There is no source
    generator, no interface-attribute model, no `Task<T> GetPropertyAsync()`
    style generated members. Every message is written and read by hand. (The
    upstream family's higher-level Tmds.DBus package and its code generator
    are not part of this port.)

  - NO PROPERTY OR OBJECT-MANAGER HELPERS. `org.freedesktop.DBus.Properties`
    and `org.freedesktop.DBus.ObjectManager` are ordinary interfaces you
    call and implement yourself; only their introspection XML is provided.

  - NO NAME OWNERSHIP API. `RequestName` and `ReleaseName` are not wrapped;
    call them on `org.freedesktop.DBus` yourself.

  - NO BUS DAEMON. It does not start, spawn or embed `dbus-daemon`, and it
    does not implement the bus side of the protocol (only the peer side,
    plus the `Peer` and `Introspectable` replies).

  - NO SERVER-SIDE LISTENING SOCKET. It connects out to an address; it does
    not accept inbound D-Bus connections.

  - NOT A GENERAL IPC LIBRARY. It is D-Bus, and Linux D-Bus at that: no
    Windows named-pipe transport story, no strong-name signing, no
    netstandard target.

  - NO AUTOMATIC RECONNECT SEMANTICS BEYOND `AutoConnect`, and no message
    queueing while disconnected — `TrySendMessage` simply returns false.


WORKING EXAMPLES ON GITHUB
==========================

The test suite is the executable specification for everything above:

  Method calls, replies, errors, method handlers, signal delivery,
  SynchronizationContext behaviour, observer/handler exception semantics:
    https://github.com/ellisnet/CodeBrix.Platform.LinuxDBus/blob/main/tests/CodeBrix.Platform.LinuxDBus.Tests/ConnectionTests.cs

  Signal sender verification against a real bus, plus a worked
  RequestName / ReleaseName pair written as extension methods:
    https://github.com/ellisnet/CodeBrix.Platform.LinuxDBus/blob/main/tests/CodeBrix.Platform.LinuxDBus.Tests/SignalOwnerTests.cs

  Reader coverage for every wire type, including handles and variants:
    https://github.com/ellisnet/CodeBrix.Platform.LinuxDBus/blob/main/tests/CodeBrix.Platform.LinuxDBus.Tests/ReaderTests.cs

  MessageWriter coverage for every wire type:
    https://github.com/ellisnet/CodeBrix.Platform.LinuxDBus/blob/main/tests/CodeBrix.Platform.LinuxDBus.Tests/WriterTests.cs

  VariantValue construction, typed retrieval and round-trips:
    https://github.com/ellisnet/CodeBrix.Platform.LinuxDBus/blob/main/tests/CodeBrix.Platform.LinuxDBus.Tests/VariantValueTests.cs

  Introspection XML generation:
    https://github.com/ellisnet/CodeBrix.Platform.LinuxDBus/blob/main/tests/CodeBrix.Platform.LinuxDBus.Tests/IntrospectionXmlTests.cs

  Connecting over each transport (unix, unix-abstract, tcp) and supplying
  your own already-connected Stream via ClientConnectionOptions:
    https://github.com/ellisnet/CodeBrix.Platform.LinuxDBus/blob/main/tests/CodeBrix.Platform.LinuxDBus.Tests/TransportTests.cs

  DBusException / error-reply behaviour:
    https://github.com/ellisnet/CodeBrix.Platform.LinuxDBus/blob/main/tests/CodeBrix.Platform.LinuxDBus.Tests/ExceptionTests.cs

  All tests:
    https://github.com/ellisnet/CodeBrix.Platform.LinuxDBus/tree/main/tests/CodeBrix.Platform.LinuxDBus.Tests


QUICK REFERENCE CARD
====================

    PackageId ........ CodeBrix.Platform.LinuxDBus.MitLicenseForever
    Namespace ........ using CodeBrix.Platform.LinuxDBus;
    Target ........... .NET 10 or later, Linux, MIT, no dependencies

  CONNECT
    string addr = Address.Session;         // or Address.System; may be null
    using var c = new Connection(addr);         // IDisposable, NOT async
    await c.ConnectAsync();                     // no CancellationToken
    Connection.Session / Connection.System      // shared + AutoConnect

  BUILD A MESSAGE  (in a non-async local function)
    using var w = c.GetMessageWriter();
    w.WriteMethodCallHeader(destination:, path:, @interface:, member:,
                            signature:, flags:);
    w.WriteString(...); w.WriteUInt32(...); w.WriteDictionary(...);
    MessageBuffer buffer = w.CreateMessage();

  CALL
    await c.CallMethodAsync(buffer);                        // no reply
    T v = await c.CallMethodAsync(buffer,
              (Message m, object s) => m.GetBodyReader().ReadX());
    c.TrySendMessage(buffer);                               // signals, no-reply

  READ A REPLY / SIGNAL BODY
    Reader r = message.GetBodyReader();
    r.ReadString() / ReadInt32() / ReadUInt32() / ReadDouble() / ReadBool()
    r.ReadArrayOfString() / ReadArrayOfInt32() / ReadArrayOfVariantValue()
    r.ReadDictionaryOfStringToVariantValue()
    r.ReadVariantValue()
    ArrayEnd e = r.ReadArrayStart(DBusType.Struct);
    while (r.HasNext(e)) { ... }

  SIGNALS
    var rule = new MatchRule { Type = MessageType.Signal, Sender = ...,
                               Path = ..., Interface = ..., Member = ... };
    IDisposable sub = await c.AddMatchAsync(rule, reader, handler,
                                            ObserverFlags.EmitOnDispose);
    sub.Dispose();                              // unsubscribe

  SERVE OBJECTS
    class H : IMethodHandler {
        public string Path => "/com/example/Obj";
        public bool RunMethodHandlerSynchronously(Message m) => true;
        public ValueTask HandleMethodAsync(MethodContext ctx) { ... }
    }
    c.AddMethodHandler(new H());
    using var rw = ctx.CreateReplyWriter("s");
    rw.WriteString(result); ctx.Reply(rw.CreateMessage());
    ctx.ReplyError("com.example.Error.X", "message");

  VARIANTS
    VariantValue.String(s) / Int32(i) / Bool(b) / Array(items) / Struct(a, b)
    vv.Type, vv.ItemType, vv.KeyType, vv.ValueType, vv.Count
    vv.GetString() / GetInt32() / GetArray<T>() / GetDictionary<K,V>()
    vv.GetItem(i) / GetDictionaryEntry(i) / GetVariantValue()

  EXCEPTIONS
    DBusException (ErrorName, ErrorMessage) ... peer replied with an error
    ConnectException ......................... could not connect
    DisconnectedException .................... connection ended
    ProtocolException ........................ malformed wire data
    ActionException.IsDisposed(ex) ........... orderly shutdown, not a fault

  REMEMBER
    Reader/MessageWriter are ref structs — no async, no lambdas, no fields.
    Message is pooled — never store it; it is not IDisposable.
    MessageBuffer is single-use and belongs to its own connection.

================================================================================
END OF AGENT-README: CodeBrix.Platform.LinuxDBus
================================================================================
