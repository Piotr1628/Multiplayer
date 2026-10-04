using Multiplayer.Client;
using MP = Multiplayer.Client.Multiplayer;
using Multiplayer.Client.Desyncs;
using Multiplayer.Common;
using NUnit.Framework;
using System.Runtime.InteropServices;
namespace DesyncSafetyTests;
public class PostfixTest
{
    [SetUp]
    public void Reset()
    {
        DeferredStackTracing.Reset();
        DeferredStackTracing.ignoreTraces = 0;
        DeferredStackTracingImpl.Throw = false;
        DeferredStackTracingImpl.Calls = 0;
        DeferredStackTracingImpl.OnTrace = null;
        MP.game = new();
        MP.Client = new();
        MP.IsReplay = false;
        MP.Ticking = true;
        Native.LmfPtr = 1;
        StackTraceLogItemRaw.Borrowed = StackTraceLogItemRaw.Returned = 0;
        TestRuntimeInformation.ProcessArchitecture = Architecture.X64;
        ServerLog.error = null;
    }
    [Test]
    public void TraceFailureDoesNotAbortRngConsumerOrLeakItemAndStopsRetrying()
    {
        DeferredStackTracingImpl.Throw = true;
        bool consumerCompleted = false;
        Assert.DoesNotThrow(() => { DeferredStackTracing.Postfix(); consumerCompleted = true; });
        DeferredStackTracing.Postfix();
        Assert.That(consumerCompleted, Is.True);
        Assert.That(DeferredStackTracingImpl.Calls, Is.EqualTo(1));
        Assert.That(StackTraceLogItemRaw.Returned, Is.EqualTo(StackTraceLogItemRaw.Borrowed));
        Assert.That(MP.game.sync.ComparisonDisabled, Is.True);
        Assert.That(MP.game.sync.Recorded, Is.Zero);
        Assert.That(DeferredStackTracing.acc, Is.Zero);
    }
    [Test]
    public void ThrowingLogCallbackCannotEscapeFailureBoundary()
    {
        DeferredStackTracingImpl.Throw = true;
        ServerLog.error = _ => throw new InvalidOperationException("Logging mod failure");
        Assert.DoesNotThrow(DeferredStackTracing.Postfix);
    }
    [Test]
    public void TeardownDuringFailedTraceCannotThrowFromErrorHandler()
    {
        DeferredStackTracingImpl.Throw = true;
        DeferredStackTracingImpl.OnTrace = () => MP.game = null;
        Assert.DoesNotThrow(DeferredStackTracing.Postfix);
        Assert.That(StackTraceLogItemRaw.Returned, Is.EqualTo(1));
    }
    [Test]
    public void NewGameCanTryTracingAgain()
    {
        DeferredStackTracingImpl.Throw = true;
        DeferredStackTracing.Postfix();
        DeferredStackTracing.Reset();
        MP.game = new();
        DeferredStackTracingImpl.Throw = false;
        DeferredStackTracing.Postfix();
        Assert.That(MP.game.sync.Recorded, Is.EqualTo(1));
        Assert.That(DeferredStackTracing.acc, Is.EqualTo(1));
    }
    [Test]
    public void Arm64NeverEntersNativeWalker()
    {
        TestRuntimeInformation.ProcessArchitecture = Architecture.Arm64;
        DeferredStackTracingImpl.Throw = true;
        DeferredStackTracing.Postfix();
        Assert.That(DeferredStackTracingImpl.Calls, Is.Zero);
        Assert.That(StackTraceLogItemRaw.Borrowed, Is.Zero);
    }
}
