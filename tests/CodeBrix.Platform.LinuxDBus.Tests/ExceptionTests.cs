using System;
using System.Threading.Tasks;
using Xunit;
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
using Nerdbank.Streams;

#pragma warning disable CS0618 // Using obsolete members

namespace CodeBrix.Platform.LinuxDBus.Tests //was previously: CodeBrix.Platform.LinuxDBus.Tests
{
    public class ExceptionTests
    {
        [Fact]
        public async Task ObserverDisposed()
        {
            var connections = PairedConnection.CreatePair();
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            TaskCompletionSource<Exception> tcs = new();

            var disposable = await conn1.AddMatchAsync(
                new MatchRule(), (Message message, object state) => "", (Exception ex, string s, object s1, object s2) =>
                {
                    tcs.SetResult(ex);
                });

            disposable.Dispose();

            Exception ex = await tcs.Task;
            Assert.True(ActionException.IsObserverDisposed(ex));
            Assert.True(ActionException.IsDisposed(ex));
        }

        [Fact]
        public async Task ConnectionDisposed()
        {
            var connections = PairedConnection.CreatePair();
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            TaskCompletionSource<Exception> tcs = new();

            var disposable = await conn1.AddMatchAsync(
                new MatchRule(), (Message message, object state) => "", (Exception ex, string s, object s1, object s2) =>
                {
                    tcs.SetResult(ex);
                });

            conn1.Dispose();

            Exception ex = await tcs.Task;
            Assert.True(ActionException.IsConnectionDisposed(ex));
            Assert.True(ActionException.IsDisposed(ex));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanOptOutObserverDisposedEmit(bool optIn)
        {
            var connections = PairedConnection.CreatePair();
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            Exception exception = null;
            var disposable = await conn1.AddMatchAsync(
                new MatchRule(), (Message message, object state) => "", (Exception ex, string s, object s1, object s2) =>
                {
                    exception ??= ex;
                }, null, null, synchronizationContext: null, optIn ? ObserverFlags.EmitOnObserverDispose : ObserverFlags.None);

            disposable.Dispose();

            if (optIn)
            {
                Assert.NotNull(exception);
            }
            else
            {
                Assert.Null(exception);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanOptOutConnectionDisposedEmit(bool optIn)
        {
            var connections = PairedConnection.CreatePair();
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            Exception exception = null;
            TaskCompletionSource<Exception> tcs = new();

            var disposable = await conn1.AddMatchAsync(
                new MatchRule(), (Message message, object state) => "", (Exception ex, string s, object s1, object s2) =>
                {
                    exception ??= ex;
                }, null, null, synchronizationContext: null, optIn ? ObserverFlags.EmitOnConnectionDispose : ObserverFlags.None);

            conn1.Dispose();

            if (optIn)
            {
                Assert.NotNull(exception);
            }
            else
            {
                Assert.Null(exception);
            }
        }
    }
}