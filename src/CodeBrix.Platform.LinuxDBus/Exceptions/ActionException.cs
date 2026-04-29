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
/// <summary>Action Exception.</summary>
public static class ActionException
{
    // Exception used when the IDisposable returned by AddMatchAsync gets disposed.
    /// <summary>Is Observer Disposed.</summary>
    public static bool IsObserverDisposed(Exception exception)
        => object.ReferenceEquals(exception, DBusConnection.ObserverDisposedException);

    // Exception used when the Connection gets disposed.
    /// <summary>Is Connection Disposed.</summary>
    public static bool IsConnectionDisposed(Exception exception)
        // note: Connection.DisposedException is only ever used as an InnerException of DisconnectedException,
        //       so we directly check for that.
        => object.ReferenceEquals(exception?.InnerException, Connection.DisposedException);

    /// <summary>Is Disposed.</summary>
    public static bool IsDisposed(Exception exception)
        => IsObserverDisposed(exception) || IsConnectionDisposed(exception);
}