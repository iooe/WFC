using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Size = System.Windows.Size;

namespace WFC.Services.System;

public class VisualHelper : IVisualHelper
{
    public void CaptureElementToPng(UIElement element, string filePath, int width, int height)
    {
        // Measure and arrange the element
        element.Measure(new Size(width, height));
        element.Arrange(new Rect(0, 0, width, height));

        // Create a render target bitmap
        var renderBitmap = new RenderTargetBitmap(
            width, height, 96, 96, PixelFormats.Pbgra32);
        renderBitmap.Render(element);

        // Create a PNG encoder
        BitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

        // Save to file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            encoder.Save(stream);
        }
    }
}