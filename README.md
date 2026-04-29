# CodeBrix.Platform.LinuxDBus

A fully managed, drop-in replacement for the `Tmds.DBus.Protocol` NuGet package, targeting .NET 10. CodeBrix.Platform.LinuxDBus is a low-level D-Bus protocol library for Linux: it connects to the session/system bus (or any transport the D-Bus specification allows), sends and receives raw D-Bus messages, and exposes the wire-level reader/writer primitives that higher-level consumers build on top of.

This port is based on the upstream `Tmds.DBus.Protocol` v0.21.3, which includes the fixes for advisory [GHSA-xrw6-gwf8-vvr9](https://github.com/tmds/Tmds.DBus/security/advisories/GHSA-xrw6-gwf8-vvr9) / CVE-2026-39959 (High severity, CVSS 7.1). Earlier v0.21.2 of the upstream package is flagged as vulnerable on NuGet; this port carries the patched source.
CodeBrix.Platform.LinuxDBus has no dependencies other than .NET, and is provided as a .NET 10 library and associated `CodeBrix.Platform.LinuxDBus.MitLicenseForever` NuGet package.

CodeBrix.Platform.LinuxDBus supports applications and assemblies that target Microsoft .NET version 10.0 and later.
Microsoft .NET version 10.0 is a Long-Term Supported (LTS) version of .NET, and was released on Nov 11, 2025; and will be actively supported by Microsoft until Nov 14, 2028.
Please update your C#/.NET code and projects to the latest LTS version of Microsoft .NET.

## CodeBrix.Platform.LinuxDBus supports:

* Connecting to any D-Bus bus — session bus, system bus, or an arbitrary address string
* Sending and awaiting D-Bus method calls
* Subscribing to D-Bus signals via `MatchRule` / `AddMatchAsync`
* Registering method handlers to expose D-Bus objects
* Reading and writing every D-Bus wire-level type: basic types, arrays, dictionaries, structs, variants, object paths, signatures, and Unix file descriptors
* Safe variant handling via `VariantValue` / `Variant`
* Generating D-Bus introspection XML from registered object paths

## Sample Code

### Connecting to the session bus and calling `org.freedesktop.DBus.ListNames`

```csharp
using CodeBrix.Platform.LinuxDBus;

await using var connection = new Connection(Address.Session!);
await connection.ConnectAsync();

using var message = connection.CreateMessage(
    destination: "org.freedesktop.DBus",
    path:        "/org/freedesktop/DBus",
    @interface:  "org.freedesktop.DBus",
    member:      "ListNames");

using var reply = await connection.CallMethodAsync(message);
var reader = reply.GetBodyReader();
string[] names = reader.ReadArrayOfString();

foreach (var name in names)
{
    Console.WriteLine(name);
}
```

## License

The project is licensed under the MIT License. see: https://en.wikipedia.org/wiki/MIT_License
