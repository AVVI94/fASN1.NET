using fASN1.NET.Tests.Infrastructure;

namespace fASN1.NET.Tests;

/// <summary>
/// Guards the parser's memory use on small documents. The budgets are about 10% above what parsing
/// allocates today (on .NET 8, x64), which is almost entirely the returned tree itself.
/// </summary>
public class AllocationTests
{
    [Theory]
    [InlineData("file:der_05", 11_000)] // GitHub TLS certificate from the README, 1,191 bytes
    [InlineData("file:der_09", 17_000)] // 2,181-byte certificate
    [InlineData("file:der_06", 5_000)]  // certificate request from the benchmark, 1,194 bytes
    public void Parse_StaysWithinBudget(string name, long budgetBytes)
    {
        var data = TestCorpus.Get(name);
        for (int i = 0; i < 5; i++)
            Asn1Serializer.Deserialize(data);

        const int runs = 50;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < runs; i++)
            Asn1Serializer.Deserialize(data);
        var perParse = (GC.GetAllocatedBytesForCurrentThread() - before) / runs;

        Assert.True(perParse <= budgetBytes, $"parsing {name} allocated {perParse} B, budget {budgetBytes} B");
    }
}
