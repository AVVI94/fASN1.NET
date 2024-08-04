using System.Collections.Generic;


namespace fASN1.NET.Tags;

public class BitString : ITag
{
    public BitString(byte[]? content = null, IList<ITag>? children = null)
    {
        if (content is not null)
        {
            var _ = CalculateUnusedBits(content);
            Content = [(byte)_, .. content];
        }
        else
            Content = [];
        Children = children ?? new List<ITag>();
    }
    public BitString(byte content) : this([content], null) { }

    public int TagNumber { get; } = 3;
    public string TagName { get; } = Tag.BitString.ToString2();
    public int TagClass { get; } = 0;
    public bool IsConstructed => Content.Length == 0 && Children.Count > 0;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }

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
