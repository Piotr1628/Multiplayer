extern alias ClientAssembly;
using System.Reflection;
using ClientAssembly::Multiplayer.Client;

namespace Tests;

public class TraceFailureComparisonTest
{
    [Test]
    public void DisablingIncompleteTracesPreservesAllSimulationStateEvidence()
    {
        var coordinator = new SyncCoordinator();
        var local = new ClientSyncOpinion(1);
        var remote = new ClientSyncOpinion(1);
        local.desyncStackTraceHashes.Add(10);
        remote.desyncStackTraceHashes.AddRange([10, 20]);
        local.worldRandomStates.Add(100);
        remote.worldRandomStates.Add(100);
        typeof(SyncCoordinator).GetField("currentOpinion", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(coordinator, local);

        local.commandRandomStates.Add(7);
        local.GetRandomStatesForMap(3).Add(9);
        coordinator.DisableStackTraceComparison();
        coordinator.DisableStackTraceComparison();
        Assert.That(local.desyncStackTraceHashes, Is.Empty);
        Assert.That(local.worldRandomStates, Is.EqualTo(new uint[] { 100 }));
        Assert.That(local.commandRandomStates, Is.EqualTo(new uint[] { 7 }));
        Assert.That(local.GetRandomStatesForMap(3), Is.EqualTo(new uint[] { 9 }));
        Assert.That(remote.desyncStackTraceHashes, Is.EqualTo(new[] { 10, 20 }));
    }
}
