using System.Collections.Concurrent;
using System.Numerics;

namespace TLECrawler.Helpers.TypeHelper;

public static class BinaryHelper
{
    public static string HashToString(this byte[] hash) =>
        BitConverter.ToString(hash).Replace("-", " ").RemoveWhitespaces();

    public static byte[] Check(string hash)
    {
        byte[] byteArray = new byte[hash.Length / 2];
        for (int i = 0; i < hash.Length; i += 2)
        {
            byteArray[i / 2] = Convert.ToByte(hash.Substring(i, 2), 16);
        }
        return byteArray;
    }
    public static string CheckBack(byte[] hash)
    {
       return BitConverter.ToString(hash).Replace("-", " ").RemoveWhitespaces();
    }
    public static bool ContainsIn(this byte[] target, List<byte[]> templates)
    {
        for (int i = 0; i < templates.Count; i++)
        {
            if (target.SimpleEqualityCheck(templates[i]!))
            {
                return true;
            }
        }
        return false;
    }
    public static bool ContainsIn(this byte[] target, ConcurrentBag<byte[]> templates)
    {
        foreach (byte[] hash in templates) 
        {            
            if (target.SimpleEqualityCheck(hash))
            {
                return true;
            }
        }
        return false;
    }
    public static bool IsInclude(this List<byte[]> templates, byte[] target)
    {
        for (int i = 0; i < templates.Count; i++)
        {
            if (target.SimpleEqualityCheck(templates[i]!))
            {
                return true;
            }
        }
        return false;
    }
    public static bool SimpleEqualityCheck(this byte[] one, byte[] another) =>
        new Span<byte>(one).SequenceEqual(new Span<byte>(another));


    public static unsafe bool CompareBinaryEqualityUnsafe(byte[] firstArray, byte[] secondArray)
    {
        if (firstArray == null || secondArray == null || firstArray.Length != secondArray.Length)
            return false;

        var arrayLength = firstArray.Length;
        var vectorSize = Vector<byte>.Count;
        fixed (byte* pbtr1 = firstArray, pbtr2 = secondArray)
        {
            var i = 0;
            for (; i <= arrayLength - vectorSize; i += vectorSize)
            {
                if (!VectorEquality(pbtr1 + i, pbtr2 + i))
                    return false;
            }
            for (; i < arrayLength; i++)
            {
                if (pbtr1[i] != pbtr2[i])
                    return false;
            }
        }
        return true;
    }
    private static unsafe bool VectorEquality(byte* firstPointer, byte* secondPointer)
    {
        var firstVector = *(Vector<byte>*)firstPointer;
        var secondVector = *(Vector<byte>*)secondPointer;
        return Vector.EqualsAll(firstVector, secondVector);
    }
}
