using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

[Serializable]
public class LevelTerrainData
{
    //Replace it with optimized tree?
    [SerializeField, HideInInspector]
    public TreeInstanceData[] Trees;

    public Texture2D LayerDiffuseTexture;

    public FirebaseLevelTerrainData ToFirebaseLevel()
    {
        FirebaseLevelTerrainData ltd = new FirebaseLevelTerrainData();
        OptimizedTree[] optimizedTrees = Trees
    .Select(t => t.ToOptimized())
    .ToArray();

        // Compress and serialize
        byte[] compressed = TreeCompressor.CompressTrees(optimizedTrees);
        ltd.Base64Data = Convert.ToBase64String(compressed);
        if (LayerDiffuseTexture != null)
        {
            Texture2D decompressedTexture = LayerDiffuseTexture.DeCompress();

            // Apply compression techniques with better quality
            ltd.DiffuseTextureBytes = CompressTextureBytes(decompressedTexture);

            // Store dimensions for reconstruction
            ltd.TextureWidth = decompressedTexture.width;
            ltd.TextureHeight = decompressedTexture.height;

            // Clean up
            UnityEngine.Object.DestroyImmediate(decompressedTexture);
        }
        return ltd;
    }

    private byte[] CompressTextureBytes(Texture2D texture)
    {
        // 1. Resize if too large but with better filtering
        Texture2D resizedTexture = ResizeTextureIfNeeded(texture, 1024);

        // 2. Ensure we're working with a high-quality base texture
        Texture2D highQualityTexture = EnsureHighQualityTexture(resizedTexture);

        // 3. Choose the best compression method based on texture content
        byte[] imageBytes = ChooseOptimalEncoding(highQualityTexture);

        // 4. Apply additional compression
        byte[] compressedBytes = CompressBytes(imageBytes);

        // Clean up
        if (resizedTexture != texture) UnityEngine.Object.DestroyImmediate(resizedTexture);
        if (highQualityTexture != resizedTexture) UnityEngine.Object.DestroyImmediate(highQualityTexture);

        return compressedBytes;
    }

    private Texture2D ResizeTextureIfNeeded(Texture2D source, int maxSize)
    {
        if (source.width <= maxSize && source.height <= maxSize)
            return source;

        float scale = Mathf.Min((float)maxSize / source.width, (float)maxSize / source.height);
        int newWidth = Mathf.RoundToInt(source.width * scale);
        int newHeight = Mathf.RoundToInt(source.height * scale);

        return ResizeTextureWithFiltering(source, newWidth, newHeight);
    }

    private Texture2D ResizeTextureWithFiltering(Texture2D source, int newWidth, int newHeight)
    {
        // Use a temporary render texture with better filtering
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
        rt.filterMode = FilterMode.Trilinear;
        RenderTexture.active = rt;

        // Use a material with better sampling for the blit operation
        Material blitMaterial = new Material(Shader.Find("Hidden/BlitWithGamma"));
        Graphics.Blit(source, rt, blitMaterial);

        Texture2D newTexture = new Texture2D(newWidth, newHeight, TextureFormat.ARGB32, false, true); // linear = true
        newTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        newTexture.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        UnityEngine.Object.DestroyImmediate(blitMaterial);

        return newTexture;
    }

    private Texture2D EnsureHighQualityTexture(Texture2D source)
    {
        // If texture is already in a high-quality format, return it
        if (source.format == TextureFormat.ARGB32 && source.mipmapCount == 0)
            return source;

        // Convert to high-quality format
        Texture2D highQualityTexture = new Texture2D(source.width, source.height, TextureFormat.ARGB32, false, true);
        highQualityTexture.SetPixels(source.GetPixels());
        highQualityTexture.Apply();
        return highQualityTexture;
    }

    private byte[] ChooseOptimalEncoding(Texture2D texture)
    {
        // Analyze texture to determine best encoding method
        bool hasAlpha = HasTransparency(texture);
        bool isPhotographic = IsPhotographic(texture);

        if (hasAlpha)
        {
            // Use PNG for textures with alpha (better quality with transparency)
            return texture.EncodeToPNG();
        }
        else if (isPhotographic)
        {
            // Use higher quality JPG for photographic content
            return texture.EncodeToJPG(85); // Higher quality than 75
        }
        else
        {
            // Use PNG for non-photographic content without alpha (sharper edges)
            return texture.EncodeToPNG();
        }
    }

    private bool HasTransparency(Texture2D texture)
    {
        // Quick check for alpha channel usage
        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < Mathf.Min(100, pixels.Length); i += 10) // Sample pixels
        {
            if (pixels[i].a < 255)
                return true;
        }
        return false;
    }

    private bool IsPhotographic(Texture2D texture)
    {
        // Simple heuristic: photographic images tend to have more color variety
        // and less sharp edges compared to synthetic images
        // This is a simplified approach - you might want to customize this
        Color32[] pixels = texture.GetPixels32();
        int colorVariation = 0;
        Color32 prevPixel = pixels[0];

        for (int i = 1; i < Mathf.Min(1000, pixels.Length); i += 10)
        {
            if (!ColorsSimilar(prevPixel, pixels[i]))
                colorVariation++;

            prevPixel = pixels[i];
        }

        // If many color variations, likely photographic
        return colorVariation > 20;
    }

    private bool ColorsSimilar(Color32 a, Color32 b, int threshold = 30)
    {
        return Math.Abs(a.r - b.r) < threshold &&
               Math.Abs(a.g - b.g) < threshold &&
               Math.Abs(a.b - b.b) < threshold;
    }

    private byte[] CompressBytes(byte[] data)
    {
        using (var output = new MemoryStream())
        {
            using (var gzip = new GZipStream(output, System.IO.Compression.CompressionLevel.Optimal))
            {
                gzip.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }
    }
}