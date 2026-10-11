using fASN1.NET.Tests.Infrastructure;

namespace fASN1.NET.Tests;

/// <summary>
/// Guards the parser's memory use on small documents. The budgets are about 5% above what parsing
/// allocates today (on .NET 8, x64), which is exactly the returned tree.
/// </summary>
public class AllocationTests
{
    [Theory]
    [InlineData("file:der_05", 7_300)] // GitHub TLS certificate from the README, 1,191 bytes
    [InlineData("file:der_09", 11_700)] // 2,181-byte certificate
    [InlineData("file:der_06", 3_500)]  // certificate request from the benchmark, 1,194 bytes
    public void Parse_StaysWithinBudget(string name, long budgetBytes)
    {
        var data = TestCorpus.Get(name);
        for (int i = 0; i < 5; i++)
            Asn1Serializer.Deserialize(data);

        const int RUNS = 50;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < RUNS; i++)
            Asn1Serializer.Deserialize(data);
        var perParse = (GC.GetAllocatedBytesForCurrentThread() - before) / RUNS;

        Assert.True(perParse <= budgetBytes, $"parsing {name} allocated {perParse} B, budget {budgetBytes} B");
    }

    [Fact]
    public void Parse_InvalidInput_AllocatesLittleMoreThanTheErrorMessage()
    {
        // A certificate cut off in its last byte: the whole structure is scanned and rejected without
        // creating tags, so only the error string is allocated.
        var data = TestCorpus.Get("file:der_05");
        data = data.AsSpan(0, data.Length - 1).ToArray();
        for (int i = 0; i < 5; i++)
            Asn1Serializer.TryDeserialize(data, out _, out _);

        const int RUNS = 50;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < RUNS; i++)
            Asn1Serializer.TryDeserialize(data, out _, out _);
        var perParse = (GC.GetAllocatedBytesForCurrentThread() - before) / RUNS;

        Assert.True(perParse <= 300, $"rejecting a truncated certificate allocated {perParse} B");
    }
}
