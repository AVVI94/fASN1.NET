using System.Diagnostics;
using fASN1.NET.Oid;

namespace fASN1.NET.Tests;
public class OidEncoderTests
{
    static Dictionary<string, OID> _oids = OID.OidDictionary;
    [Fact]
    public void GetBytes_GetBytesForAllItemsInDictionaryAndConvertsThemBackToString()
    {
        foreach (var oid in _oids)
        {
            var bytes = OidEncoder.GetBytes(oid.Value.Value);
            var str = OidEncoder.GetString(bytes);
            if (oid.Key != str)
                Debugger.Break();
            Assert.Equal(oid.Key, str);
        }
    }
}
