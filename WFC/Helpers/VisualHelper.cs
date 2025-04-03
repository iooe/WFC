using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Size = System.Windows.Size;

namespace WFC.Helpers
{
    public static class VisualHelper
    {
        /// <summary>
        /// Captures a visual element as a PNG image file
        /// </summary>
        public static void CaptureElementToPng(UIElement element, string filePath, int width, int height)
        {
            // Create the render bitmap
            var renderBitmap = new RenderTargetBitmap(
                width, height, 96, 96, PixelFormats.Pbgra32);

            // Measure and arrange the element if needed
            element.Measure(new Size(width, height));
            element.Arrange(new Rect(0, 0, width, height));

            // Render the element
            renderBitmap.Render(element);

            // Create PNG encoder
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            // Save to file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }
    }
}