using System.Collections.Generic;


namespace fASN1.NET.Tags;

public class BitString : TagBase
{
    public BitString(byte[]? content = null, IList<ITag>? children = null) : base(WithUnusedBits(content), children)
    {
    }
    public BitString(byte content) : this([content], null) { }

    public override int TagNumber => 3;
    public override string TagName => "BitString";
    public override int TagClass => 0;

    private static byte[]? WithUnusedBits(byte[]? content)
    {
        if (content is null)
            return null;
        var _ = CalculateUnusedBits(content);
        return [(byte)_, .. content];
    }

    public static int CalculateUnusedBits(byte[] bitStringBytes)
    {
        byte lastByte = bitStringBytes[bitStringBytes.Length - 1];
        int unusedBits = 0;
        for (int i = 0; i < 8; i++)
        {
            if ((lastByte & 1 << i) != 0)
            {
                unusedBits = 7 - i;
                break;
            }
        }
        return unusedBits;
    }
}
