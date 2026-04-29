================================================================================
AGENT-README: CodeBrix.Platform.LinuxDBus
A Comprehensive Guide for AI Coding Agents
================================================================================

OVERVIEW
--------
CodeBrix.Platform.LinuxDBus is a low-level D-Bus protocol library for
Linux, targeting .NET 10. It speaks the D-Bus wire protocol directly: it
connects to the session bus, the system bus, or any transport the D-Bus
specification allows; sends and receives raw D-Bus messages; subscribes
to signals; registers method handlers to expose D-Bus objects; and
exposes the wire-level reader/writer primitives higher-level consumers
build on top of.

CodeBrix.Platform.LinuxDBus is a fully-managed, .NET 10-only port of the
upstream `Tmds.DBus.Protocol` NuGet package (version 0.21.3) by Tom
Deseyn and contributors. It is intended as a drop-in replacement for
`Tmds.DBus.Protocol` in .NET 10 projects: the public API shape is
preserved, only the namespace changes from `Tmds.DBus.Protocol` to
`CodeBrix.Platform.LinuxDBus`. Every file that was derived from the
upstream project carries a `//was previously: Tmds.DBus.Protocol...;`
provenance comment on its `namespace` line, and the upstream project is
credited in THIRD-PARTY-NOTICES.txt.


INSTALLATION
------------
NuGet Package: CodeBrix.Platform.LinuxDBus.MitLicenseForever
Dependencies:  (none other than the .NET 10 shared framework)

    dotnet add package CodeBrix.Platform.LinuxDBus.MitLicenseForever

IMPORTANT: The NuGet package name is
CodeBrix.Platform.LinuxDBus.MitLicenseForever (NOT
CodeBrix.Platform.LinuxDBus). The primary namespace is
CodeBrix.Platform.LinuxDBus.

Requirements: .NET 10.0 or higher, Linux runtime (the library uses
Unix domain sockets and requires a D-Bus daemon to be running).
License: MIT License.


SECURITY POSTURE
----------------
CodeBrix.Platform.LinuxDBus is based on Tmds.DBus.Protocol v0.21.3,
which incorporates the fixes for advisory GHSA-xrw6-gwf8-vvr9 /
CVE-2026-39959 (High severity, CVSS 7.1). The upstream v0.21.2 is
flagged on NuGet as having a known high-severity vulnerability; this
port carries the patched source from v0.21.3.

The three attack vectors fixed in v0.21.3 are:

  1. Signal-sender spoofing — before the fix, a subscription to a
     signal from (for example) "org.freedesktop.NetworkManager"
     delivered ANY signal whose `Sender` header claimed that name,
     so any peer on the bus could impersonate well-known names. The
     fix resolves each subscribed well-known name to its current
     unique-name owner via `GetNameOwner`, watches
     `NameOwnerChanged`, and only delivers signals whose actual
     unique-name sender matches.

  2. Unix file-descriptor resource exhaustion + fd spillover — the
     D-Bus spec caps the number of file descriptors attached to a
     single message at 16. Before the fix, sendmsg/recvmsg had no
     such cap, so a malicious peer could force unbounded fd
     allocation; received fds could also "spill" from one message
     into the next, leading to fd-accounting confusion. The fix
     enforces `ProtocolConstants.MaxMessageFileDescriptors = 16`,
     sets `MSG_CMSG_CLOEXEC` on received fds (so they are
     close-on-exec immediately), rejects `MSG_CTRUNC`, and discards
     any leftover fds between messages. The SASL auth line length
     is also capped at 512 bytes (`MaxAuthLineLength`) to bound
     auth-handshake memory.

  3. Denial-of-service via unhandled exceptions — before the fix, a
     malformed message body (or any throw inside a user AddMatch
     callback) could escape to the SynchronizationContext and crash
     the host process. The fix wraps every AddMatch reader/handler
     call in try/catch and routes exceptions through a private
     Disconnect(ex) helper.

The full test suite (in `tests/CodeBrix.Platform.LinuxDBus.Tests/`)
passes on .NET 10, including the SignalOwnerTests that exercise the
signal-sender check end-to-end against `PairedConnection` and a real
`dbus-daemon`.


PORTING NOTES
-------------
CodeBrix.Platform.LinuxDBus is a port of the upstream Tmds.DBus.Protocol
library at tag `rel/0.21.3` (https://github.com/tmds/Tmds.DBus). The
following changes were made during the port:

  1. Target framework narrowed from
     `netstandard2.0;netstandard2.1;net6.0;net8.0` to `net10.0` only.
     Every `#if NETSTANDARD2_0 / NETSTANDARD2_1 / NET6_0 / NET7_0` block
     was stripped to the live branch; the remaining `Netstandard2_0` /
     `Netstandard2_1` extension methods that survive on .NET 10 were
     consolidated into `Internal/NetExtensions.cs`.

  2. Namespace renamed from `Tmds.DBus.Protocol` to
     `CodeBrix.Platform.LinuxDBus`. Every file carries a
     `//was previously: Tmds.DBus.Protocol...;` provenance comment.

  3. The upstream `GlobalUsings.cs` was deleted; every global `using` was
     promoted to an explicit file-scoped `using` directive. Global
     `using` is forbidden family-wide in CodeBrix.

  4. `<Nullable>enable</Nullable>` was removed; every `?` annotation on
     a reference type and every null-forgiveness `!` operator were
     stripped. Value-type `Nullable<T>` (`int?`, `MessageType?`, etc.)
     is preserved.

  5. The `Tmds.DBus.Protocol.Tests` upstream test project was ported
     from xUnit (v2) to xUnit v3, with `SilverAssertions` added as a
     test dependency. `SkippableFact` / `SkippableTheory` /
     `SkipTestException` were replaced with xUnit v3's built-in
     `Assert.Skip(...)`. `TestContext.Current.CancellationToken` is
     threaded through every cancellable call inside tests.

  6. `<GenerateDocumentationFile>true</GenerateDocumentationFile>` is
     enabled; every public and protected-on-unsealed member carries a
     first-pass XML doc comment. Many summaries are auto-generated from
     the member name and are intentionally simple — they will benefit
     from hand-editing in follow-up passes.

  7. `SignAssembly`, `AssemblyOriginatorKeyFile`, `PublicSign`, and the
     embedded `sign.snk` resource were removed. This port is not
     strong-name signed.

  8. The `XunitSkip/` folder from the upstream test project was dropped
     (xUnit v3 has built-in skip support).


KEY NAMESPACE
-------------
    using CodeBrix.Platform.LinuxDBus;

Everything in the library lives in this single namespace — same shape
as the upstream `Tmds.DBus.Protocol` for drop-in compatibility.


================================================================================

CORE API REFERENCE
==================

CONNECTION CLASS - MAIN ENTRY POINT
------------------------------------
The `Connection` class is the primary public API. It owns the transport,
the reader/writer pipeline, and the method-dispatch table. It implements
`IAsyncDisposable` — always use `await using` patterns.

Construction:
    var connection = new Connection(Address.Session);
    var connection = new Connection(Address.System);
    var connection = new Connection("unix:path=/run/user/1000/bus");
    var connection = new Connection(new ClientConnectionOptions(addr) {
        AutoConnect = true,
    });

Connecting:
    await connection.ConnectAsync();
    await connection.ConnectAsync(cancellationToken);

Making method calls:
    using var message = connection.CreateMessage(
        destination: "org.freedesktop.DBus",
        path:        "/org/freedesktop/DBus",
        @interface:  "org.freedesktop.DBus",
        member:      "ListNames");
    using var reply = await connection.CallMethodAsync(message);
    var reader = reply.GetBodyReader();
    string[] names = reader.ReadArrayOfString();

Subscribing to signals:
    var rule = new MatchRule {
        Type      = MessageType.Signal,
        Interface = "org.freedesktop.DBus",
        Member    = "NameOwnerChanged",
    };
    var subscription = await connection.AddMatchAsync<(string, string, string)>(
        rule,
        (Message m, object _) => {
            var r = m.GetBodyReader();
            return (r.ReadString(), r.ReadString(), r.ReadString());
        },
        (ex, args, _, _) => {
            if (ex == null) Console.WriteLine($"{args.Item1} changed owner");
        },
        null, null,
        emitOnCapturedContext: false,
        ObserverFlags.None);

Registering a method handler (exposing an object on the bus):
    connection.AddMethodHandler(new MyHandler());

Dispose / disconnect:
    await connection.DisposeAsync();


ADDRESS CLASS
-------------
Static helpers for retrieving the well-known D-Bus bus addresses:

    string Address.Session   // $DBUS_SESSION_BUS_ADDRESS or the default
    string Address.System    // $DBUS_SYSTEM_BUS_ADDRESS or the default

Either may be null if the corresponding bus cannot be located.


MESSAGE / MESSAGEBUFFER / MESSAGEWRITER / READER
-------------------------------------------------
`Message` is a reference-counted wrapper around a received D-Bus message.
Dispose it when done.

`MessageBuffer` is an outgoing message buffer, produced by
`MessageWriter.CreateMessage()` and sent via `Connection`.

`MessageWriter` is the high-performance writer that produces the D-Bus
wire format. It is a ref struct — use it within the same method that
creates it, and always call `CreateMessage()` or `Dispose()`.

    using var writer = connection.GetMessageWriter();
    writer.WriteMethodCallHeader(
        destination: "...",
        path:        "...",
        @interface:  "...",
        member:      "...",
        signature:   "s");
    writer.WriteString("hello");
    using var message = writer.CreateMessage();

`Reader` is the matching ref struct reader. Obtain one from `Message`:
    var reader = message.GetBodyReader();
    string s = reader.ReadString();
    int i = reader.ReadInt32();

Per-type Read/Write partials exist for every D-Bus wire type:
  - Basic: byte, int16/32/64, uint16/32/64, double, bool, string,
    ObjectPath, Signature, UnixFd
  - Composite: Array, Dict (dictionary), Struct, Variant
  - Handle (Unix file descriptor) pairs


WIRE-LEVEL TYPES
----------------
  - `ObjectPath` (readonly struct): wraps a D-Bus object path (`/foo/bar`)
  - `Signature` (readonly struct): wraps a D-Bus signature (`a{sv}`)
  - `Variant` (readonly ref struct): lightweight variant view
  - `VariantValue` (readonly struct): owned variant value
  - `Array<T>` / `Dict<TKey,TValue>` / `Struct<T1..T10>`: strongly typed
    wrappers over the generic reader/writer
  - `UnixFdCollection`: manages a list of unix file descriptors received
    with a message


EXCEPTIONS
----------
  - `DBusException`     — base exception with `ErrorName` and `ErrorMessage`
  - `ConnectException`  — transport/connect failure
  - `DisconnectedException` — connection was closed
  - `ProtocolException` — D-Bus wire-level error
  - `ActionException`   — error thrown from a user handler


CODING CONVENTIONS (CodeBrix family)
------------------------------------
  - Target framework: `net10.0` only. No multi-targeting.
  - NRT off (no `<Nullable>enable</Nullable>`). Reference types never
    use `?`; value-type `Nullable<T>` (`int?`, `MyEnum?`) is fine.
  - No global usings (`<ImplicitUsings>` off, `GlobalUsings.cs` forbidden).
    Every `using` is file-scoped and fully qualified.
  - File-scoped namespace declarations (`namespace X;`), not block-scoped.
  - `<GenerateDocumentationFile>true</GenerateDocumentationFile>` — every
    public and protected-on-unsealed member needs an XML doc comment.
  - `<NoWarn>` is forbidden. Warnings are fixed at source.
  - xUnit v3 + SilverAssertions for tests (`.Should()...` fluent style).
  - `TestContext.Current.CancellationToken` threaded through every
    cancellable call inside tests.


================================================================================

ARCHITECTURE
============

The library's source code lives under `src/CodeBrix.Platform.LinuxDBus/`
and is organized into disk sub-folders for readability (all files share
a single `CodeBrix.Platform.LinuxDBus` namespace for drop-in compatibility
with the upstream):

    src/CodeBrix.Platform.LinuxDBus/
      (root)           Connection, Address, MethodContext, IMethodHandler,
                       ClientConnectionOptions — main entry points

      Messages/        Message, MessageBuffer, MessagePool, MessageStream,
                       MessageBufferPool, IMessageStream, DisposableMessage,
                       MessageHeader

      Reading/         Reader.cs and Reader.*.cs partials, SignatureReader

      Writing/         MessageWriter.cs and MessageWriter.*.cs partials,
                       IDBusWritable

      Types/           The D-Bus wire types: Array, Dict, ObjectPath,
                       Signature, Struct, Variant, VariantValue,
                       VariantValueConverter, VariantExtensions,
                       IVariantValueConvertable, UnixFdCollection,
                       CloseSafeHandle

      Enumerations/    DBusType, MessageFlags, MessageType, ObserverFlags,
                       VariantValueType

      Exceptions/      ActionException, ConnectException, DBusException,
                       DisconnectedException, ProtocolException

      Internal/        Implementation-detail helpers: AddressReader,
                       Constants, DBusConnection, DBusEnvironment,
                       IntrospectionXml, MatchRule, PathNodeDictionary,
                       PlatformDetection, ProtocolConstants,
                       SocketExtensions, StringBuilderExtensions,
                       ThrowHelper, TypeModel, NetExtensions, and the
                       third-party `NerdbankStreamsSequence.cs` (Sequence<T>
                       by Andrew Arnott, MIT).


================================================================================

TESTING
=======

Tests live under `tests/CodeBrix.Platform.LinuxDBus.Tests/` and use
xUnit v3 with SilverAssertions. The suite covers:

  - VariantValue round-trips across every D-Bus wire type
  - Reader / MessageWriter round-trips
  - Signature parsing / validation (SignatureReader)
  - PathNodeDictionary behaviour
  - IntrospectionXml generation
  - Connection lifecycle (via PairedConnection in-memory pair)
  - Full end-to-end transport tests (Unix, UnixAbstract, Tcp) against a
    real `dbus-daemon` subprocess

Running the tests:

    dotnet test CodeBrix.Platform.LinuxDBus.slnx

Transport tests require the `dbus-daemon` binary to be installed on the
host (usually `apt install dbus` / `dnf install dbus-daemon`). The Tcp
transport test is auto-skipped when running under SELinux enforcement.

Tests that depend on a Linux-only resource — the `dbus-daemon`
subprocess (TransportTests, most SignalOwnerTests) or the `libc`
native library for Unix file descriptors (`ReaderTests.ReadHandle`,
`ReaderTests.ReadHandleRaw`, `VariantValueTests.UnixFd`) — are gated
with xUnit v3's conditional-skip attributes
(`Skip = "...", SkipUnless = nameof(PlatformGate.IsLinux),
SkipType = typeof(PlatformGate)`). The `PlatformGate` helper lives in
`tests/CodeBrix.Platform.LinuxDBus.Tests/PlatformGate.cs` and exposes
a single `static bool IsLinux` derived from
`RuntimeInformation.IsOSPlatform(OSPlatform.Linux)`. The result is that
the full suite runs to completion on Windows and macOS — every
Linux-only test reports as Skipped (with a human-readable reason)
rather than failing. When adding a new test that requires `dbus-daemon`
or `libc`, copy that same attribute pattern.


================================================================================
END OF AGENT-README: CodeBrix.Platform.LinuxDBus
================================================================================
