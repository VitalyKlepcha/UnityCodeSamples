using System.IO;
using System.IO.Compression;

public static class TreeCompressor
{
    public static byte[] CompressTrees(OptimizedTree[] trees)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(trees.Length);

            foreach (var tree in trees)
            {
                writer.Write(tree.positionX);
                writer.Write(tree.positionY);
                writer.Write(tree.positionZ);
                writer.Write(tree.widthScale);
                writer.Write(tree.heightScale);
                writer.Write(tree.rotation);
                writer.Write(tree.color);
                writer.Write(tree.lightmapColor);
                writer.Write(tree.prototypeIndex);
            }

            return Compress(stream.ToArray());
        }
    }

    public static OptimizedTree[] DecompressTrees(byte[] data)
    {
        byte[] decompressed = Decompress(data);
        using (var stream = new MemoryStream(decompressed))
        using (var reader = new BinaryReader(stream))
        {
            int count = reader.ReadInt32();
            var trees = new OptimizedTree[count];

            for (int i = 0; i < count; i++)
            {
                trees[i] = new OptimizedTree
                {
                    positionX = reader.ReadUInt16(),
                    positionY = reader.ReadUInt16(),
                    positionZ = reader.ReadUInt16(),
                    widthScale = reader.ReadByte(),
                    heightScale = reader.ReadByte(),
                    rotation = reader.ReadUInt16(),
                    color = reader.ReadUInt16(),
                    lightmapColor = reader.ReadUInt16(),
                    prototypeIndex = reader.ReadByte()
                };
            }
            return trees;
        }
    }

    private static byte[] Compress(byte[] data)
    {
        using (var output = new MemoryStream())
        {
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
            {
                gzip.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }
    }

    private static byte[] Decompress(byte[] data)
    {
        using (var input = new MemoryStream(data))
        using (var gzip = new GZipStream(input, CompressionMode.Decompress))
        using (var output = new MemoryStream())
        {
            gzip.CopyTo(output);
            return output.ToArray();
        }
    }
}