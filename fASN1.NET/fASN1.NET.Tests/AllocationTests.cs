using fASN1.NET.Tests.Infrastructure;

namespace fASN1.NET.Tests;

/// <summary>
/// Guards the parser's memory use on small documents. The budgets are about 5% above what parsing
/// allocates today (on .NET 8, x64): the returned tree plus the MemoryStream wrapping a byte array.
/// </summary>
public class AllocationTests
{
    [Theory]
    [InlineData("file:der_05", 10_000)] // GitHub TLS certificate from the README, 1,191 bytes
    [InlineData("file:der_09", 16_000)] // 2,181-byte certificate
    [InlineData("file:der_06", 4_700)]  // certificate request from the benchmark, 1,194 bytes
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

    [Fact]
    public void Parse_InvalidInput_AllocatesLittleMoreThanTheErrorMessage()
    {
        // A certificate cut off in its last byte: the whole structure is scanned and rejected without
        // creating tags, so only the MemoryStream and the error string are allocated.
        var data = TestCorpus.Get("file:der_05");
        data = data.AsSpan(0, data.Length - 1).ToArray();
        for (int i = 0; i < 5; i++)
            Asn1Serializer.TryDeserialize(data, out _, out _);

        const int runs = 50;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < runs; i++)
            Asn1Serializer.TryDeserialize(data, out _, out _);
        var perParse = (GC.GetAllocatedBytesForCurrentThread() - before) / runs;

        Assert.True(perParse <= 400, $"rejecting a truncated certificate allocated {perParse} B");
    }
}
