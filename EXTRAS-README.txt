================================================================================
EXTRAS-README: CodeBrix.Platform.LinuxDBus
Samples, tools and other content in this repository that is not part of a
NuGet package
================================================================================

This repository contains NO samples, NO tools and NO demo applications. The
only non-package content is the test project.


TEST PROJECT
============

    tests/CodeBrix.Platform.LinuxDBus.Tests/

An xUnit v3 test project (with SilverAssertions) that exercises the library
end to end. It is not packed and is not published; it is referenced only by
the solution.

Running it:

    dotnet test CodeBrix.Platform.LinuxDBus.slnx

What it demonstrates, beyond verifying correctness, is a complete worked
example of every consumer scenario the package supports — method calls and
replies, error replies, method handlers, signal subscription and delivery,
variant round-trips and each transport. AGENT-README.txt links the
individual files under "WORKING EXAMPLES ON GITHUB"; they are the best place
to look for usage patterns that the documentation summarises.


OPTIONAL / GATED TESTS
======================

Some tests need resources that only exist on a Linux host, and they SKIP
(they do not fail) elsewhere:

  - `dbus-daemon` must be installed for the transport tests and for most of
    the signal-owner tests. On a Debian-based system: `apt install dbus`.
  - `libc` is needed for the Unix file-descriptor tests
    (ReaderTests.ReadHandle, ReaderTests.ReadHandleRaw,
    VariantValueTests.UnixFd).
  - The TCP transport case skips itself at run time under SELinux
    enforcement, because the daemon cannot obtain the peer's security
    context over TCP and closes the connection.

There are no environment variables to set and no opt-in switches: the gates
are automatic.


TEST HELPERS WORTH KNOWING ABOUT
================================

These live in the test project and are not part of the package, but they are
the pieces you would copy when writing new tests:

  PlatformGate.cs
      A single `static bool IsLinux`. It is the `SkipType`/`SkipUnless`
      target for every Linux-only test.

  DBusDaemon.cs
      Starts a real `dbus-daemon` subprocess with a temporary session-bus
      configuration (anonymous auth allowed) over unix, unix-abstract or tcp,
      exposes its `Address`, and shuts it down on `Dispose()`. Also exposes
      `IsSELinux`.

  PairedConnection.cs
      `PairedConnection.CreatePair()` returns two `Connection` objects wired
      to each other through an in-memory `IMessageStream` pair, with no
      daemon, no sockets and no bus. This is how most of the connection,
      method-handler and signal tests run: fast, deterministic, and available
      on every OS. Note that the two ends are peers rather than bus clients,
      so bus-only behaviour (name ownership, AddMatch subscription) is not
      exercised there — those cases use DBusDaemon instead.

================================================================================
END OF EXTRAS-README: CodeBrix.Platform.LinuxDBus
================================================================================
