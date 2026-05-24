using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

internal static class ImageFixtures
{
    public static void SaveSolidPng(string path, Color color)
    {
        var pixels = new byte[16];
        for (var pixel = 0; pixel < pixels.Length; pixel += 4)
        {
            pixels[pixel] = color.B; pixels[pixel + 1] = color.G; pixels[pixel + 2] = color.R; pixels[pixel + 3] = color.A;
        }
        var texture = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null, pixels, 8);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(texture));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
