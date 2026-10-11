using System.Collections.Concurrent;
using fASN1.NET.Oid;

namespace fASN1.NET.Tests;

/// <summary>
/// The shared OID registry is used by every parse and every thread, and the OIDs it is asked for come from input.
/// </summary>
public class OidRegistryTests
{
    [Fact]
    public void GetOrCreate_KnownOid_ReturnsTheRegisteredOne()
    {
        var oid = OID.GetOrCreate(OID.KEY_USAGE);

        Assert.Same(OID.OidDictionary[OID.KEY_USAGE], oid);
        Assert.NotEmpty(oid.FriendlyName);
    }

    [Fact]
    public void GetOrCreate_UnknownOid_IsNotAddedToTheRegistry()
    {
        // Every distinct OID in untrusted input used to be added for good, so input could grow the registry without limit.
        var countBefore = OID.OidDictionary.Count;

        for (int i = 0; i < 10_000; i++)
        {
            var oid = OID.GetOrCreate(OidEncoder.GetBytes($"1.3.6.1.4.1.99999.{i}"));
            Assert.Equal($"1.3.6.1.4.1.99999.{i}", oid.Value);
            Assert.Equal("", oid.FriendlyName);
        }

        Assert.False(OID.OidDictionary.ContainsKey("1.3.6.1.4.1.99999.0"));
        Assert.True(OID.OidDictionary.Count <= countBefore + 1, "only OIDs registered by other tests may appear");
    }

    [Fact]
    public void GetOrCreate_FromManyThreads_NeverFails()
    {
        // Concurrent writes to the plain Dictionary could throw or corrupt it, and enumerating it while another
        // thread added an OID threw "Collection was modified".
        Parallel.For(0, 64, thread =>
        {
            for (int i = 0; i < 2_000; i++)
            {
                _ = OID.GetOrCreate($"1.3.6.1.4.1.88888.{thread}.{i}");
                _ = OID.GetOrCreate(OID.COMMON_NAME);
                if (i % 500 == 0)
                {
                    _ = OID.OidDictionary.Count(pair => pair.Key != pair.Value.Value);
                }
            }
        });

        Assert.All(OID.OidDictionary, pair => Assert.Equal(pair.Key, pair.Value.Value));
    }

    [Fact]
    public void Register_AddsTheFriendlyName()
    {
        const string VALUE = "1.3.6.1.4.1.77777.1";

        OID.Register(new OID(VALUE, "testOid", "OidRegistryTests"));

        Assert.Equal("testOid", OID.GetOrCreate(VALUE).FriendlyName);
        Assert.Equal("testOid", OID.GetOrCreate(OidEncoder.GetBytes(VALUE)).FriendlyName);
        Assert.Throws<ArgumentNullException>(() => OID.Register(null!));
    }

    [Fact]
    public void OidDictionary_CannotBeCastToAWritableDictionary()
    {
        // The registry is a ConcurrentDictionary; handing it out as IReadOnlyDictionary let a cast write to it.
        var dictionary = OID.OidDictionary;

        Assert.False(dictionary is ConcurrentDictionary<string, OID>);
        var writable = Assert.IsAssignableFrom<IDictionary<string, OID>>(dictionary);
        Assert.True(writable.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => writable["1.3.6.1.4.1.66666.1"] = new OID("1.3.6.1.4.1.66666.1", "x", "y"));
        Assert.False(OID.OidDictionary.ContainsKey("1.3.6.1.4.1.66666.1"));
    }

    [Fact]
    public void OidDictionary_ShowsOidsRegisteredLater_AndEnumeratesWhileOthersRegister()
    {
        var dictionary = OID.OidDictionary;

        Parallel.For(0, 16, thread =>
        {
            for (int i = 0; i < 200; i++)
            {
                if (thread % 2 == 0)
                {
                    OID.Register(new OID($"1.3.6.1.4.1.55555.{thread}.{i}", "concurrent", "OidRegistryTests"));
                }
                else
                {
                    Assert.All(dictionary, pair => Assert.Equal(pair.Key, pair.Value.Value));
                }
            }
        });

        Assert.Equal("concurrent", dictionary["1.3.6.1.4.1.55555.0.199"].FriendlyName);
    }
}
