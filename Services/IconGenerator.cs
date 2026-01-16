using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SLImage = SixLabors.ImageSharp.Image;

namespace OmniShell.Services;

/// <summary>
/// Generates colored folder icons by extracting and tinting the Windows folder icon
/// </summary>
public class IconGenerator
{
    private readonly string _cacheDirectory;
    private readonly object _cacheLock = new object();
    
    #region Windows API for Icon Extraction
    
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
    
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, int nIcons);
    
    #endregion
    
    // Predefined folder colors with HSL-friendly values
    public static readonly Dictionary<string, string> FolderColors = new()
    {
        { "red", "#E53935" },
        { "orange", "#FB8C00" },
        { "yellow", "#FDD835" },
        { "green", "#43A047" },
        { "blue", "#1E88E5" },
        { "purple", "#8E24AA" },
        { "pink", "#D81B60" },
        { "gray", "#757575" },
        { "brown", "#6D4C41" },
        { "cyan", "#00ACC1" }
    };
    
    public IconGenerator(string cacheDirectory)
    {
        _cacheDirectory = cacheDirectory;
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }
    
    /// <summary>
    /// Get or generate a colored folder icon
    /// </summary>
    public string GetColoredFolderIcon(string colorId)
    {
        var iconPath = Path.Combine(_cacheDirectory, $"folder_{colorId}.ico");
        
        // Thread-safe cache check and generation
        lock (_cacheLock)
        {
            // Check if already exists
            if (File.Exists(iconPath))
            {
                return iconPath;
            }
            
            if (!FolderColors.TryGetValue(colorId.ToLower(), out var hexColor))
            {
                throw new ArgumentException($"Unknown color: {colorId}");
            }
            
            GenerateColoredFolderIcon(iconPath, hexColor);
            return iconPath;
        }
    }
    
    /// <summary>
    /// Generate a colored folder icon by extracting Windows folder icon and applying tint
    /// </summary>
    private void GenerateColoredFolderIcon(string outputPath, string hexColor)
    {
        var targetColor = ParseHexColor(hexColor);
        
        // Extract Windows folder icon at multiple sizes
        var sizes = new[] { 16, 32, 48, 256 };
        var tintedImages = new List<Image<Rgba32>>();
        
        try
        {
            foreach (var size in sizes)
            {
                var image = ExtractAndTintWindowsFolderIcon(size, targetColor);
                if (image != null)
                {
                    tintedImages.Add(image);
                }
            }
            
            // If extraction failed, fall back to generated icons
            if (tintedImages.Count == 0)
            {
                foreach (var size in sizes)
                {
                    tintedImages.Add(CreateFallbackFolderImage(size, targetColor));
                }
            }
            
            SaveAsIco(outputPath, tintedImages);
        }
        finally
        {
            foreach (var img in tintedImages)
            {
                img.Dispose();
            }
        }
    }
    
    /// <summary>
    /// Extract Windows folder icon and apply color tint
    /// </summary>
    private Image<Rgba32>? ExtractAndTintWindowsFolderIcon(int size, Rgba32 targetColor)
    {
        try
        {
            // Try to extract folder icon from shell32.dll (index 3 is closed folder, 4 is open folder)
            // On Windows 11, imageres.dll has better icons
            string[] iconSources = {
                @"C:\Windows\System32\imageres.dll",
                @"C:\Windows\System32\shell32.dll"
            };
            
            // Folder icon indices to try
            int[] folderIndices = { 3, 4, 5 }; // Common folder icon indices
            
            foreach (var dllPath in iconSources)
            {
                if (!File.Exists(dllPath)) continue;
                
                foreach (var iconIndex in folderIndices)
                {
                    var icon = ExtractIconFromDll(dllPath, iconIndex, size);
                    if (icon != null)
                    {
                        var tinted = ApplyColorTint(icon, targetColor);
                        icon.Dispose();
                        return tinted;
                    }
                }
            }
        }
        catch
        {
            // Fall through to return null
        }
        
        return null;
    }
    
    /// <summary>
    /// Extract an icon from a DLL file at a specific size
    /// </summary>
    private Image<Rgba32>? ExtractIconFromDll(string dllPath, int iconIndex, int desiredSize)
    {
        // Use ExtractIconEx P/Invoke to extract icons from DLL
        IntPtr[] largeIcons = new IntPtr[1];
        IntPtr[] smallIcons = new IntPtr[1];
        
        try
        {
            int count = ExtractIconEx(dllPath, iconIndex, largeIcons, smallIcons, 1);
            if (count > 0 && largeIcons[0] != IntPtr.Zero)
            {
                try
                {
                    // Create icon from handle - this creates a copy, so we must destroy the original handle
                    var icon = System.Drawing.Icon.FromHandle(largeIcons[0]);
                    try
                    {
                        using var bitmap = icon.ToBitmap();
                        
                        // Resize to desired size if needed
                        if (bitmap.Width != desiredSize || bitmap.Height != desiredSize)
                        {
                            using var resized = new Bitmap(bitmap, new System.Drawing.Size(desiredSize, desiredSize));
                            return ConvertBitmapToImageSharp(resized);
                        }
                        else
                        {
                            return ConvertBitmapToImageSharp(bitmap);
                        }
                    }
                    finally
                    {
                        icon.Dispose();
                    }
                }
                finally
                {
                    // Always destroy icon handles to prevent memory leaks
                    if (largeIcons[0] != IntPtr.Zero) DestroyIcon(largeIcons[0]);
                    if (smallIcons[0] != IntPtr.Zero) DestroyIcon(smallIcons[0]);
                }
            }
        }
        catch
        {
            // Clean up handles even on exception
            try
            {
                if (largeIcons[0] != IntPtr.Zero) DestroyIcon(largeIcons[0]);
                if (smallIcons[0] != IntPtr.Zero) DestroyIcon(smallIcons[0]);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Convert System.Drawing.Bitmap to ImageSharp Image
    /// </summary>
    private Image<Rgba32> ConvertBitmapToImageSharp(Bitmap bitmap)
    {
        var image = new Image<Rgba32>(bitmap.Width, bitmap.Height);
        
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                image[x, y] = new Rgba32(pixel.R, pixel.G, pixel.B, pixel.A);
            }
        }
        
        return image;
    }
    
    /// <summary>
    /// Apply a color tint to an image while preserving luminosity and transparency
    /// </summary>
    private Image<Rgba32> ApplyColorTint(Image<Rgba32> source, Rgba32 targetColor)
    {
        var result = source.Clone();
        
        // Convert target color to HSL for tinting
        RgbToHsl(targetColor.R, targetColor.G, targetColor.B, out float targetH, out float targetS, out float _);
        
        result.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    
                    // Skip fully transparent pixels
                    if (pixel.A == 0) continue;
                    
                    // Convert current pixel to HSL
                    RgbToHsl(pixel.R, pixel.G, pixel.B, out float h, out float s, out float l);
                    
                    // Apply target hue and saturation, keep original luminosity
                    // Reduce saturation slightly to maintain some of the original shading
                    float newS = targetS * 0.8f + s * 0.2f;
                    
                    // Convert back to RGB
                    HslToRgb(targetH, newS, l, out byte r, out byte g, out byte b);
                    
                    row[x] = new Rgba32(r, g, b, pixel.A);
                }
            }
        });
        
        return result;
    }
    
    /// <summary>
    /// Fallback: Create a simple folder-shaped image if Windows icon extraction fails
    /// </summary>
    private Image<Rgba32> CreateFallbackFolderImage(int size, Rgba32 color)
    {
        var image = new Image<Rgba32>(size, size, new Rgba32(0, 0, 0, 0));
        
        float scale = size / 64f;
        
        var darkerColor = new Rgba32(
            (byte)Math.Max(0, color.R - 40),
            (byte)Math.Max(0, color.G - 40),
            (byte)Math.Max(0, color.B - 40),
            255
        );
        
        var lighterColor = new Rgba32(
            (byte)Math.Min(255, color.R + 30),
            (byte)Math.Min(255, color.G + 30),
            (byte)Math.Min(255, color.B + 30),
            255
        );
        
        // Simple folder shape using polygons
        image.Mutate(ctx =>
        {
            // Folder body
            var bodyRect = new SixLabors.ImageSharp.Drawing.RectangularPolygon(
                4 * scale, 12 * scale, 52 * scale, 44 * scale);
            ctx.Fill(color, bodyRect);
            
            // Folder tab
            var tabRect = new SixLabors.ImageSharp.Drawing.RectangularPolygon(
                4 * scale, 8 * scale, 20 * scale, 6 * scale);
            ctx.Fill(darkerColor, tabRect);
            
            // Highlight
            var highlightRect = new SixLabors.ImageSharp.Drawing.RectangularPolygon(
                4 * scale, 12 * scale, 52 * scale, 6 * scale);
            ctx.Fill(lighterColor, highlightRect);
        });
        
        return image;
    }
    
    /// <summary>
    /// Save images as ICO file
    /// </summary>
    private void SaveAsIco(string outputPath, List<Image<Rgba32>> images)
    {
        using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);
        
        // ICO Header
        bw.Write((short)0);           // Reserved
        bw.Write((short)1);           // Type: 1 = ICO
        bw.Write((short)images.Count); // Number of images
        
        int headerSize = 6 + (16 * images.Count);
        var imageDataList = new List<byte[]>();
        
        int currentOffset = headerSize;
        
        foreach (var image in images)
        {
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            var pngData = ms.ToArray();
            imageDataList.Add(pngData);
            
            // ICO Directory Entry
            bw.Write((byte)(image.Width >= 256 ? 0 : image.Width));
            bw.Write((byte)(image.Height >= 256 ? 0 : image.Height));
            bw.Write((byte)0);        // Color palette
            bw.Write((byte)0);        // Reserved
            bw.Write((short)1);       // Color planes
            bw.Write((short)32);      // Bits per pixel
            bw.Write(pngData.Length); // Size of image data
            bw.Write(currentOffset);  // Offset to image data
            
            currentOffset += pngData.Length;
        }
        
        foreach (var data in imageDataList)
        {
            bw.Write(data);
        }
    }
    
    #region Color Conversion Helpers
    
    private static Rgba32 ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
        byte b = Convert.ToByte(hex.Substring(4, 2), 16);
        return new Rgba32(r, g, b, 255);
    }
    
    private static void RgbToHsl(byte r, byte g, byte b, out float h, out float s, out float l)
    {
        float rf = r / 255f;
        float gf = g / 255f;
        float bf = b / 255f;
        
        float max = Math.Max(rf, Math.Max(gf, bf));
        float min = Math.Min(rf, Math.Min(gf, bf));
        float delta = max - min;
        
        l = (max + min) / 2f;
        
        if (delta == 0)
        {
            h = 0;
            s = 0;
        }
        else
        {
            s = l > 0.5f ? delta / (2f - max - min) : delta / (max + min);
            
            if (max == rf)
                h = ((gf - bf) / delta + (gf < bf ? 6 : 0)) / 6f;
            else if (max == gf)
                h = ((bf - rf) / delta + 2) / 6f;
            else
                h = ((rf - gf) / delta + 4) / 6f;
        }
    }
    
    private static void HslToRgb(float h, float s, float l, out byte r, out byte g, out byte b)
    {
        float rf, gf, bf;
        
        if (s == 0)
        {
            rf = gf = bf = l;
        }
        else
        {
            float q = l < 0.5f ? l * (1 + s) : l + s - l * s;
            float p = 2 * l - q;
            
            rf = HueToRgb(p, q, h + 1f / 3f);
            gf = HueToRgb(p, q, h);
            bf = HueToRgb(p, q, h - 1f / 3f);
        }
        
        r = (byte)Math.Round(rf * 255);
        g = (byte)Math.Round(gf * 255);
        b = (byte)Math.Round(bf * 255);
    }
    
    private static float HueToRgb(float p, float q, float t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1f / 6f) return p + (q - p) * 6 * t;
        if (t < 1f / 2f) return q;
        if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6;
        return p;
    }
    
    #endregion
    
    /// <summary>
    /// Get all available color options
    /// </summary>
    public static IEnumerable<(string Id, string Name, string HexColor)> GetAvailableColors()
    {
        return FolderColors.Select(kv => (
            Id: kv.Key,
            Name: char.ToUpper(kv.Key[0]) + kv.Key[1..],
            HexColor: kv.Value
        ));
    }
    
    /// <summary>
    /// Clear the icon cache to force regeneration
    /// </summary>
    public void ClearCache()
    {
        if (Directory.Exists(_cacheDirectory))
        {
            foreach (var file in Directory.GetFiles(_cacheDirectory, "folder_*.ico"))
            {
                try { File.Delete(file); } catch { }
            }
        }
    }
}
