using Multiplayer.Client.Desyncs;

namespace Tests;

public class AddrTableTest
{
    [Test]
    public void NewlyInsertedAddressesRemainReachableAcrossResizes()
    {
        var table = new AddrTable();
        const int count = 6000;
        for (int i = 1; i <= count; i++)
        {
            long address = i * 16L;
            ref var entry = ref table.GetOrCreateAddrInfo(address);
            entry.addr = address;
            entry.stackUsage = i * 8;
            entry.nameHash = i * 31;

            // Look up immediately: the old implementation returned a slot from
            // the discarded array when inserting the entry that triggered growth.
            ref var lookup = ref table.GetOrCreateAddrInfo(address);
            Assert.That(lookup.addr, Is.EqualTo(address), $"Insertion {i}");
        }

        Assert.That(table.Entries, Is.EqualTo(count));
        Assert.That(table.Count(), Is.EqualTo(count));
        for (int i = 1; i <= count; i++)
        {
            ref var entry = ref table.GetOrCreateAddrInfo(i * 16L);
            Assert.That(entry.stackUsage, Is.EqualTo(i * 8));
            Assert.That(entry.nameHash, Is.EqualTo(i * 31));
        }
        Assert.That(table.Entries, Is.EqualTo(count));
    }
}
