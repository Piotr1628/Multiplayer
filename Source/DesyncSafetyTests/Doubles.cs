// These doubles isolate the managed error boundary in the actual linked postfix.
// They do not execute Mono native stack walking or a RimWorld simulation.
global using RuntimeInformation = DesyncSafetyTests.TestRuntimeInformation;
using System.Runtime.InteropServices;
namespace DesyncSafetyTests
{
    public static class TestRuntimeInformation { public static Architecture ProcessArchitecture = Architecture.X64; }
}
namespace Multiplayer.Common
{
    public static class ServerLog
    {
        public static Action<string> error;
        public static void Error(string message) => error?.Invoke(message);
    }
    public static class MpVersion { public static bool IsDebug; }
    public enum DesyncTracingMode { None, Enabled }
}
namespace Multiplayer.Client.Patches
{
    public class EarlyPatchAttribute : Attribute { }
}
namespace Multiplayer.Client
{
    public static class Native { public static long LmfPtr = 1; }
    public static class Multiplayer
    {
        public static object Client = new();
        public static Game game = new();
        public static Settings settings = new();
        public static bool IsReplay, Ticking = true, ExecutingCmds;
    }
    public class Game { public GameComp gameComp = new(); public SyncCoordinator sync = new(); }
    public class GameComp { public bool logDesyncTraces = true; }
    public class Settings { public global::Multiplayer.Common.DesyncTracingMode desyncTracingMode = global::Multiplayer.Common.DesyncTracingMode.Enabled; }
    public class SyncCoordinator
    {
        public bool ComparisonDisabled;
        public int Recorded;
        public void DisableStackTraceComparison() => ComparisonDisabled = true;
        public void TryAddStackTraceForDesyncLogRaw(StackTraceLogItemRaw item, int depth, int hash) => Recorded++;
        public void TryAddInfoForDesyncLog(string a, string b) { }
    }
    public class StackTraceLogItemRaw : IDisposable
    {
        public long[] raw = new long[32];
        public static int Borrowed, Returned;
        public static StackTraceLogItemRaw GetFromPool() { Borrowed++; return new(); }
        public void Dispose() => Returned++;
    }
}
namespace Multiplayer.Client.Desyncs
{
    public static class DeferredStackTracingImpl
    {
        public static bool Throw;
        public static int Calls;
        public static Action OnTrace;
        public static int TraceImpl(long[] trace, ref int hash, int skipFrames)
        {
            Calls++;
            OnTrace?.Invoke();
            if (Throw) throw new InvalidOperationException("Unknown function header");
            hash = 42;
            return 3;
        }
    }
}
namespace Verse
{
    public static class Rand { public static float Value => 0; public static int Int => 0; public static Stack<int> stateStack = new(); }
    public class UniqueIDsManager { public int GetNextID() => 0; }
    public class ThingDef { public bool HasThingIDNumber; }
    public class Thing { public ThingDef def = new(); public void SpawnSetup() { } public void DeSpawn() { } }
    public class Pawn { }
    public class WildAnimalSpawner { public void WildAnimalSpawnerTick() { } }
    public class WildPlantSpawner { public void WildPlantSpawnerTick() { } }
    public class SteadyEnvironmentEffects { public void SteadyEnvironmentEffectsTick() { } }
    public class TempTerrainManager { public void Tick() { } }
}
namespace Verse.AI
{
    public class Pawn_JobTracker { public object curJob; public Verse.Pawn pawn; public void EndCurrentJob() { } }
}
namespace RimWorld { public class CompThrownFleckEmitter { public void CompTick() { } } public class IntermittentSteamSprayer { public void SteamSprayerTick() { } } }
