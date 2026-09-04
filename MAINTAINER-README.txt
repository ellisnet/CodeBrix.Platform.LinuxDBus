================================================================================
MAINTAINER-README: CodeBrix.Platform.LinuxDBus
Notes for people and agents MAINTAINING this repository — not for package
consumers
================================================================================

If you are consuming the NuGet package, read AGENT-README.txt instead. This
file is about changing the repository itself.


PURPOSE AND SCOPE
=================

This repository produces exactly one NuGet package:

    PackageId    CodeBrix.Platform.LinuxDBus.MitLicenseForever
    Project      src/CodeBrix.Platform.LinuxDBus/
    Assembly     CodeBrix.Platform.LinuxDBus
    Namespace    CodeBrix.Platform.LinuxDBus
    License      MIT
    Consumer doc AGENT-README.txt (repository root)

The library is a low-level D-Bus protocol implementation for Linux: it
connects to a bus, sends and receives raw messages, subscribes to signals,
dispatches incoming method calls to registered handlers, and exposes the
wire-level reader/writer primitives.


REPOSITORY LAYOUT
=================

    CodeBrix.Platform.LinuxDBus.slnx    solution. The Solution Items folder
                                        carries .gitignore, AGENT-README.txt,
                                        EXTRAS-README.txt, global.json,
                                        icon-codebrix-128.png, LICENSE,
                                        MAINTAINER-README.txt,
                                        README-INDEX.txt, README.md and
                                        THIRD-PARTY-NOTICES.txt; the Tests
                                        folder holds the test project
    src/CodeBrix.Platform.LinuxDBus/    the library and its csproj
    tests/CodeBrix.Platform.LinuxDBus.Tests/
                                        the xUnit v3 test project
    icon-codebrix-128.png               package icon (packed)
    LICENSE                             MIT
    THIRD-PARTY-NOTICES.txt             upstream attributions (packed)
    README.md                           human-facing overview (packed)
    AGENT-README.txt                    consumer doc (packed)
    MAINTAINER-README.txt               this file
    EXTRAS-README.txt                   non-package content
    README-INDEX.txt                    map of the README files
    global.json                         selects the Microsoft.Testing.Platform
                                        test runner; pins no SDK version

There are no samples, tools or demo applications in this repository.

SOURCE FOLDER ORGANISATION
--------------------------
Every file in the library shares the single `CodeBrix.Platform.LinuxDBus`
namespace — the sub-folders are for readability only, and MUST NOT be turned
into sub-namespaces (the flat namespace is what keeps the public API shape
compatible with the upstream library):

    (root)          Connection (partial: Connection.cs, Connection.DBus.cs),
                    Address, ConnectionOptions, ClientConnectionOptions,
                    ClientSetupResult, IMethodHandler,
                    IMethodHandlerDictionary, MethodContext,
                    InternalsVisibleTo.cs

    Messages/       Message, MessageBuffer, MessageBufferPool, MessagePool,
                    MessageStream, IMessageStream, DisposableMessage,
                    MessageHeader

    Reading/        Reader.cs plus the Reader.Array / .Basic / .Dictionary /
                    .Handle / .ReadT / .Variant partials, SignatureReader

    Writing/        MessageWriter.cs plus the MessageWriter.Array / .Basic /
                    .Dictionary / .Handle / .Header / .IntrospectionXml /
                    .Struct / .Variant / .WriteT partials, IDBusWritable

    Types/          Array, Dict, Struct, ObjectPath, Signature, Variant,
                    VariantValue, VariantValueConverter, VariantExtensions,
                    IVariantValueConvertable, UnixFdCollection,
                    CloseSafeHandle

    Enumerations/   DBusType, MessageFlags, MessageType, ObserverFlags,
                    VariantValueType

    Exceptions/     ActionException, ConnectException, DBusException,
                    DisconnectedException, ProtocolException

    Internal/       AddressReader, Constants, DBusConnection,
                    DBusEnvironment, IntrospectionXml, MatchRule,
                    NetExtensions, PathNodeDictionary, PlatformDetection,
                    ProtocolConstants, SocketExtensions,
                    StringBuilderExtensions, ThrowHelper, TypeModel, and
                    the third-party NerdbankStreamsSequence.cs
                    (Sequence<T> by Andrew Arnott, MIT, kept in its
                    original `Nerdbank.Streams` namespace)

Note that folder placement does not track accessibility: `MatchRule` and
`IntrospectionXml` live under Internal/ but are PUBLIC types, while
`UnixFdCollection` and `CloseSafeHandle` live under Types/ but are internal.
Check the declaration, not the folder, before documenting anything as public
surface.

`src/CodeBrix.Platform.LinuxDBus/InternalsVisibleTo.cs` grants
`CodeBrix.Platform.LinuxDBus.Tests` access to internals; the tests rely on
it (for example `Connection.Connect(IMessageStream)` and the paired
in-memory streams).


BUILDING
========

    dotnet restore CodeBrix.Platform.LinuxDBus.slnx
    dotnet build   CodeBrix.Platform.LinuxDBus.slnx

The library targets `net10.0` only. `AllowUnsafeBlocks` is on (the reader,
writer and VariantValue use pointers), and
`GenerateDocumentationFile` is on — CS1591 must be fixed at source, never
suppressed, and `<NoWarn>` is forbidden.

`GeneratePackageOnBuild` is true, so an ordinary build also produces a
.nupkg. See PACKAGING AND PUBLISHING for why that matters.


TESTING
=======

    dotnet test CodeBrix.Platform.LinuxDBus.slnx

THE TEST RUNNER IS Microsoft.Testing.Platform, selected by global.json at the
repository root. That file does NOT pin an SDK version, so the newest
installed .NET 10 SDK is still used; it exists solely to select the runner:

    { "test": { "runner": "Microsoft.Testing.Platform" } }

Because the setting lives in global.json rather than in the test csproj, it
applies to every `dotnet test` run anywhere in the repository, including CI.
Keep the file committed -- without it `dotnet test` silently falls back to the
older VSTest bridge.

The test project uses xUnit v3 with SilverAssertions, plus
Microsoft.NET.Test.Sdk and xunit.runner.visualstudio. There is no coverage
collector: coverlet.collector was removed from the test project.
Coverage areas:

  - VariantValue construction, typed retrieval and round-trips across every
    D-Bus wire type
  - Reader and MessageWriter round-trips, big-endian and little-endian
  - Signature parsing and validation (SignatureReader)
  - PathNodeDictionary behaviour (the object-path tree used for dispatch)
  - IntrospectionXml generation
  - Connection lifecycle over the in-memory PairedConnection pair
  - End-to-end transport tests (unix, unix-abstract, tcp) against a real
    `dbus-daemon` subprocess
  - SignalOwnerTests: the signal-sender verification fix, end to end

HOST REQUIREMENTS AND SKIP GATES
--------------------------------
Tests that need a Linux-only resource — the `dbus-daemon` subprocess
(TransportTests, most SignalOwnerTests) or `libc` for Unix file descriptors
(ReaderTests.ReadHandle, ReaderTests.ReadHandleRaw, VariantValueTests.UnixFd)
— are gated with xUnit v3's conditional-skip attributes:

    [Fact(Skip = "Requires Linux (dbus-daemon executable).",
          SkipUnless = nameof(PlatformGate.IsLinux),
          SkipType = typeof(PlatformGate))]

`PlatformGate` lives in tests/CodeBrix.Platform.LinuxDBus.Tests/
PlatformGate.cs and exposes a single `static bool IsLinux` derived from
`RuntimeInformation.IsOSPlatform(OSPlatform.Linux)`. The effect is that the
whole suite runs to completion on Windows and macOS, with the Linux-only
tests reported as Skipped (with a human-readable reason) rather than failed.
WHEN ADDING A TEST THAT NEEDS `dbus-daemon` OR `libc`, COPY THAT ATTRIBUTE
PATTERN.

On Linux, `dbus-daemon` must be installed for the transport and signal-owner
tests to run (`apt install dbus`). The TCP transport case additionally
skips itself at run time under SELinux enforcement (`DBusDaemon.IsSELinux`
checks for /sys/fs/selinux): on an SELinux host the daemon's
`getsockopt(SO_PEERSEC)` returns ENOPROTOOPT over TCP and it closes the
connection.

The test daemon is started by tests/CodeBrix.Platform.LinuxDBus.Tests/
DBusDaemon.cs, which writes a temporary session-bus config allowing
anonymous authentication and listens on the requested transport.


PACKAGING AND PUBLISHING
========================

Packing is driven entirely from
src/CodeBrix.Platform.LinuxDBus/CodeBrix.Platform.LinuxDBus.csproj; there is
no separate .nuspec and no pack script.

WHAT SHIPS IN THE NUPKG (the `None ... Pack="true"` items):

    icon-codebrix-128.png       (PackageIcon)
    README.md                   (PackageReadmeFile)
    AGENT-README.txt
    THIRD-PARTY-NOTICES.txt

MAINTAINER-README.txt, EXTRAS-README.txt and README-INDEX.txt are
repository-only files and are deliberately NOT packed.

VERSIONING SCHEME
-----------------
Date-stamped and auto-incrementing, computed from `System.DateTime.UtcNow`
at build time:

    1.<x>.<y>.<z>
      1   major     always 1 for this library
      x   minor     whole years since $(_VersionBaseYear) (2026 => 0)
      y   build     day of year, 1-based, UTC (Jan 1 = 1)
      z   revision  minute of day, UTC, 0..1439

The value is strictly increasing over time, and it is NOT SemVer — major is
pinned and minor encodes the year, so neither signals API compatibility.
Consequences to keep in mind:

  - every build produces a NEW version, and with `GeneratePackageOnBuild`
    that means a fresh .nupkg on every build;
  - two builds inside the same UTC minute produce the SAME version, so never
    publish two packages from within one minute;
  - to re-baseline the minor number, change `_VersionBaseYear` in the
    csproj.

Package metadata (id, title, description, tags, project/repository URL,
copyright, `PackageRequireLicenseAcceptance`) also lives in that csproj.
Publishing to nuget.org is a manual step; tag the repository to match the
published version.

Per family policy the CodeBrix.Platform family is shipped together as one
version and one event — do not publish this package on its own without that
coordination.


PROVENANCE AND VENDORED SOURCES
===============================

CodeBrix.Platform.LinuxDBus is a port of the upstream Tmds.DBus.Protocol
library at tag `rel/0.21.3` (https://github.com/tmds/Tmds.DBus), MIT
licensed, by Tom Deseyn and contributors, building on earlier work by Alp
Toker and others. Every ported .cs file in src/ and tests/ carries a
`//was previously: Tmds.DBus.Protocol...;` provenance comment on its
`namespace` line, and the full attribution (including the reproduced
upstream license text) is in THIRD-PARTY-NOTICES.txt.

Additionally, `Internal/NerdbankStreamsSequence.cs` holds
`Nerdbank.Streams.Sequence<T>` by Andrew Arnott (MIT), copied verbatim and
deliberately left in its original `Nerdbank.Streams` namespace. Do not
rename it and do not move it into the library namespace — the `using
Nerdbank.Streams;` directives across the source depend on it.

WHY 0.21.3 SPECIFICALLY — THE SECURITY FIXES
--------------------------------------------
Upstream 0.21.2 is flagged on nuget.org as having a known high-severity
vulnerability; 0.21.3 carries the fixes for advisory GHSA-xrw6-gwf8-vvr9 /
CVE-2026-39959 (High, CVSS 7.1). This port is taken from the patched tag.
The three attack vectors that were fixed, and how the fix works in this
source tree:

  1. SIGNAL-SENDER SPOOFING. Before the fix, a subscription to a signal from
     (say) "org.freedesktop.NetworkManager" delivered ANY signal whose
     `Sender` header claimed that name, so any peer on the bus could
     impersonate a well-known name. The fix resolves each subscribed
     well-known name to its current unique-name owner via `GetNameOwner`,
     watches `NameOwnerChanged` with an associated match, tracks the owner
     in the connection's name-owner map, and delivers a signal only when the
     actual unique-name sender matches. See `DBusConnection
     .AddWatcherUserAsync` and the SenderName bookkeeping around it.

  2. UNIX FILE-DESCRIPTOR RESOURCE EXHAUSTION AND FD SPILLOVER. The D-Bus
     specification caps the descriptors attached to one message at 16.
     Before the fix, sendmsg/recvmsg enforced no such cap, so a malicious
     peer could force unbounded fd allocation, and received descriptors
     could "spill" from one message into the next, confusing fd accounting.
     The fix enforces `ProtocolConstants.MaxMessageFileDescriptors = 16`,
     sets MSG_CMSG_CLOEXEC on received descriptors, rejects MSG_CTRUNC, and
     discards leftover descriptors between messages. The SASL auth line
     length is capped at `ProtocolConstants.MaxAuthLineLength = 512` bytes
     to bound auth-handshake memory.

  3. DENIAL OF SERVICE VIA UNHANDLED EXCEPTIONS. Before the fix, a malformed
     message body — or any throw inside a user AddMatch callback — could
     escape to the SynchronizationContext and take down the host process.
     The fix wraps every AddMatch reader/handler invocation in try/catch and
     routes the exception through the private `Disconnect(ex)` helper, so
     the connection dies but the process does not. ConnectionTests
     .ObserverExceptionInValueReaderDisposesObserver,
     .ObserverExceptionInValueHandlerDisposesObserver and
     .HandlerExceptionDisconnectsConnection pin that behaviour.

DO NOT REVERT ANY OF THE ABOVE while merging upstream changes, and do not
re-base this port onto a pre-0.21.3 upstream snapshot.

PORTING NOTES — WHAT WAS CHANGED FROM UPSTREAM
----------------------------------------------
  1. Target framework narrowed from
     `netstandard2.0;netstandard2.1;net6.0;net8.0` to `net10.0` only. Every
     `#if NETSTANDARD2_0 / NETSTANDARD2_1 / NET6_0 / NET7_0 / NETFRAMEWORK`
     block was stripped to the live branch. That removed
     `Netstandard2_0Extensions.cs`, `Netstandard2_1Extensions.cs` and the
     netstandard-only parts of the upstream `Polyfill/` folder; the
     extension methods that still matter on .NET 10 were consolidated into
     `Internal/NetExtensions.cs`.

  2. Namespace renamed from `Tmds.DBus.Protocol` to
     `CodeBrix.Platform.LinuxDBus`, with the `//was previously:` provenance
     comment on every ported file.

  3. Upstream `GlobalUsings.cs` deleted; every global `using` promoted to an
     explicit file-scoped `using` directive. Global usings are forbidden
     family-wide.

  4. `<Nullable>enable</Nullable>` removed; every `?` annotation on a
     reference type and every null-forgiveness `!` operator stripped.
     Value-type `Nullable<T>` (`int?`, `MessageType?`) is preserved.

  5. Tests ported from xUnit v2 to xUnit v3, with SilverAssertions added.
     `SkippableFact` / `SkippableTheory` / `SkipTestException` were replaced
     by xUnit v3's built-in skip support (`Assert.Skip(...)` and the
     conditional-skip attributes), and the upstream `XunitSkip/` folder was
     dropped. `TestContext.Current.CancellationToken` is threaded through
     every cancellable call inside tests.

  6. `<GenerateDocumentationFile>true</GenerateDocumentationFile>` enabled;
     every public and protected-on-unsealed member carries an XML doc
     comment. MANY SUMMARIES ARE FIRST-PASS AND AUTO-DERIVED FROM THE MEMBER
     NAME ("Call Method Async.") — they are intentionally simple and are the
     obvious target for a hand-editing pass. Improving them is welcome;
     removing them is not.

  7. `SignAssembly`, `AssemblyOriginatorKeyFile`, `PublicSign` and the
     embedded `sign.snk` resource removed. This port is NOT strong-name
     signed.

  8. Source split into the disk sub-folders listed under REPOSITORY LAYOUT,
     while keeping the single flat namespace.

MERGING FUTURE UPSTREAM CHANGES
-------------------------------
Diff the upstream tag against `rel/0.21.3`, then re-apply items 1-8 to the
changed files. Keep the flat namespace, keep NRT off, keep the provenance
comments, and add the skip gates to any new Linux-only test.


CODING CONVENTIONS
==================

  - Target framework `net10.0` only. No multi-targeting; netstandard is
    reserved family-wide for Roslyn analyzer hosts, of which this is not
    one.
  - Nullable reference types OFF. Reference types never carry `?`;
    value-type `Nullable<T>` is fine.
  - No global usings. `<ImplicitUsings>` stays off and `GlobalUsings.cs`
    must not come back; every `using` is written at the top of the file that
    needs it.
  - File-scoped namespace declarations (`namespace X;`), not block-scoped —
    in the library. (The test project still uses the block-scoped form in
    several files, inherited from the upstream layout.)
  - `<GenerateDocumentationFile>true</GenerateDocumentationFile>`; every
    public and protected-on-unsealed member needs an XML doc comment. Fix
    CS1591 at source. `<NoWarn>` is forbidden.
  - Keep the `//was previously: ...;` provenance comment when editing a
    ported file. Comment code out rather than deleting it when the intent is
    to record a divergence from upstream.
  - Tests: xUnit v3 + SilverAssertions fluent `.Should()...` style; test
    classes named `<Class>Tests.cs`; thread
    `TestContext.Current.CancellationToken` through cancellable calls
    (xUnit1051).
  - Do not add a dependency. The package's "no dependencies beyond the .NET
    shared framework" property is a feature and is advertised as such.


NOTES
=====

  - The public surface is small (about forty public types) and mirrors the
    upstream API shape deliberately. Renaming a public type or changing a
    signature breaks drop-in compatibility for consumers migrating off the
    upstream package — treat the public surface as frozen unless there is a
    specific reason.
  - Several types look public but are not: `UnixFdCollection`,
    `CloseSafeHandle`, `VariantValueConverter`, `IMessageStream` (and the
    `MessageReceivedHandler<T>` delegate nested inside it),
    `IMethodHandlerDictionary`, `AddressParser` and its nested
    `AddressEntry`. Verify the declaration before adding anything to the
    consumer documentation.
  - `Variant`, `Signature(string)` and `VariantValue.GetObjectPath()` are
    marked `[Obsolete]` upstream and remain so here. `Variant` is obsolete
    only in non-DEBUG builds (`#if !DEBUG`), which is why several source
    files carry `#pragma warning disable CS0618` — that pragma is
    load-bearing, do not remove it.
  - README.md, LICENSE, THIRD-PARTY-NOTICES.txt, the icon and the eight
    AI-agent pointer files (AGENTS.md, CLAUDE.md, .clinerules, .cursorrules,
    .cursor/rules/agent-readme.mdc, .windsurfrules,
    .github/copilot-instructions.md, .junie/guidelines.md) are maintained to
    a family-wide canonical form; do not hand-edit them in this repository
    alone.

================================================================================
END OF MAINTAINER-README: CodeBrix.Platform.LinuxDBus
================================================================================
