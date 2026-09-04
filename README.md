# CodeBrix.Platform.LinuxDBus

A fully managed, low-level D-Bus protocol library for Linux, targeting .NET 10. CodeBrix.Platform.LinuxDBus connects to the session/system bus (or any transport the D-Bus specification allows), sends and receives raw D-Bus messages, and exposes the wire-level reader/writer primitives that higher-level consumers build on top of.
CodeBrix.Platform.LinuxDBus has no dependencies other than .NET, and is provided as a .NET 10 library and associated `CodeBrix.Platform.LinuxDBus.MitLicenseForever` NuGet package.

CodeBrix.Platform.LinuxDBus supports applications and assemblies that target Microsoft .NET version 10.0 and later.
Microsoft .NET version 10.0 is a Long-Term Supported (LTS) version of .NET, and was released on Nov 11, 2025; and will be actively supported by Microsoft until Nov 14, 2028.
Please update your C#/.NET code and projects to the latest LTS version of Microsoft .NET.

## Installation

```
dotnet add package CodeBrix.Platform.LinuxDBus.MitLicenseForever
```

Note that the NuGet package ID and the namespace are different - there is no package named plain `CodeBrix.Platform.LinuxDBus`:

* NuGet package ID: `CodeBrix.Platform.LinuxDBus.MitLicenseForever`
* Assembly and primary namespace: `CodeBrix.Platform.LinuxDBus` - i.e. `using CodeBrix.Platform.LinuxDBus;`

XML documentation (IntelliSense) ships alongside the assembly. The package brings in no NuGet dependencies of its own - .NET itself is all it needs.

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
using System;
using System.Threading.Tasks;
using CodeBrix.Platform.LinuxDBus;

public static class ListNamesExample
{
    public static async Task RunAsync()
    {
        using var connection = new Connection(Address.Session!);
        await connection.ConnectAsync();

        // The generic overload returns the reply, decoded by the reader you pass in.
        string[] names = await connection.CallMethodAsync(
            CreateMessage(connection),
            (Message message, object state) =>
                message.GetBodyReader().ReadArrayOfString());

        foreach (string name in names)
        {
            Console.WriteLine(name);
        }
    }

    // MessageWriter is a ref struct, so build the message in a non-async helper.
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
```

## Documentation

The NuGet package includes `AGENT-README.txt`, a complete API reference and usage guide written for AI coding agents - point your agent at that file when it is writing code against this library.

Additional sample code and usage examples are available in the `CodeBrix.Platform.LinuxDBus.Tests` project:
https://github.com/ellisnet/CodeBrix.Platform.LinuxDBus/tree/main/tests/CodeBrix.Platform.LinuxDBus.Tests

## License

CodeBrix.Platform.LinuxDBus is licensed under the MIT License - see the
[LICENSE](https://github.com/ellisnet/CodeBrix.Platform.LinuxDBus/blob/main/LICENSE) file.

For licensing and provenance information about the open source code included in
this package, see [THIRD-PARTY-NOTICES.txt](https://github.com/ellisnet/CodeBrix.Platform.LinuxDBus/blob/main/THIRD-PARTY-NOTICES.txt).
