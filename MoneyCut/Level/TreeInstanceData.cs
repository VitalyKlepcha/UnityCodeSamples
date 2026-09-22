using System;
using UnityEngine;

/// <summary>
/// Wrapper for TreeInstance struct to support unity serialization
/// </summary>
[Serializable]
public class TreeInstanceData
{

    public Vector3 position;

    public float widthScale;

    public float heightScale;

    public float rotation;

    public Color32 color;

    public Color32 lightmapColor;

    public int prototypeIndex;

    public TreeInstance ToCoreTree()
    {
        return new TreeInstance
        {
            position = position,
            widthScale = widthScale,
            heightScale = heightScale,
            rotation = rotation,
            color = color,
            lightmapColor = lightmapColor,
            prototypeIndex = prototypeIndex
        };
    }
}

public static class TreeInstanceExstensions
{
    public static OptimizedTree ToOptimized(this TreeInstanceData treeData)
    {
        return new OptimizedTree
        {
            positionX = (ushort)(treeData.position.x * 65535f),
            positionY = (ushort)(treeData.position.y * 65535f),
            positionZ = (ushort)(treeData.position.z * 65535f),
            widthScale = (byte)Mathf.Clamp(treeData.widthScale * 10f, 0, 255),
            heightScale = (byte)Mathf.Clamp(treeData.heightScale * 10f, 0, 255),
            rotation = (ushort)(treeData.rotation / (2 * Mathf.PI) * 65535f),
            color = ColorToUShort(treeData.color),
            lightmapColor = ColorToUShort(treeData.lightmapColor),
            prototypeIndex = (byte)treeData.prototypeIndex
        };
    }

    public static TreeInstanceData ToWrappedTree(this TreeInstance tree)
    {
        return new TreeInstanceData
        {
            color = tree.color,
            widthScale = tree.widthScale,
            heightScale = tree.heightScale,
            position = tree.position,
            lightmapColor = tree.lightmapColor,
            rotation = tree.rotation,
            prototypeIndex = tree.prototypeIndex
        };
    }

    // Original to Optimized
    public static OptimizedTree ToOptimized(this TreeInstance tree)
    {
        return new OptimizedTree
        {
            positionX = (ushort)(tree.position.x * 65535f),
            positionY = (ushort)(tree.position.y * 65535f),
            positionZ = (ushort)(tree.position.z * 65535f),
            widthScale = (byte)Mathf.Clamp(tree.widthScale * 10f, 0, 255),
            heightScale = (byte)Mathf.Clamp(tree.heightScale * 10f, 0, 255),
            rotation = (ushort)(tree.rotation / (2 * Mathf.PI) * 65535f),
            color = ColorToUShort(tree.color),
            lightmapColor = ColorToUShort(tree.lightmapColor),
            prototypeIndex = (byte)tree.prototypeIndex
        };
    }

    //Optimized to Wrapped
    public static TreeInstanceData ToWrappedTree(this OptimizedTree optimized)
    {
        return new TreeInstanceData
        {
            position = new Vector3(
                optimized.positionX / 65535f,
                optimized.positionY / 65535f,
                optimized.positionZ / 65535f),
            widthScale = optimized.widthScale / 10f,
            heightScale = optimized.heightScale / 10f,
            rotation = optimized.rotation / 65535f * 2 * Mathf.PI,
            color = UShortToColor(optimized.color),
            lightmapColor = UShortToColor(optimized.lightmapColor),
            prototypeIndex = optimized.prototypeIndex
        };
    }

    // Optimized to Original
    public static TreeInstance ToTree(this OptimizedTree opt)
    {
        return new TreeInstance
        {
            position = new Vector3(
                opt.positionX / 65535f,
                opt.positionY / 65535f,
                opt.positionZ / 65535f),
            widthScale = opt.widthScale / 10f,
            heightScale = opt.heightScale / 10f,
            rotation = opt.rotation / 65535f * 2 * Mathf.PI,
            color = UShortToColor(opt.color),
            lightmapColor = UShortToColor(opt.lightmapColor),
            prototypeIndex = opt.prototypeIndex
        };
    }

    // Color32 to 16-bit (RGBA4444)
    private static ushort ColorToUShort(Color32 c)
    {
        return (ushort)(
            ((c.r >> 4) << 12) |
            ((c.g >> 4) << 8) |
            ((c.b >> 4) << 4) |
            (c.a >> 4));
    }

    // 16-bit to Color32 (RGBA4444)
    private static Color32 UShortToColor(ushort u)
    {
        return new Color32(
            (byte)(((u >> 12) & 0xF) * 17),
            (byte)(((u >> 8) & 0xF) * 17),
            (byte)(((u >> 4) & 0xF) * 17),
            (byte)((u & 0xF) * 17));
    }
}
