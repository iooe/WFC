using System.Windows;

namespace WFC.Services;

public interface IVisualHelper
{
    void CaptureElementToPng(UIElement element, string filePath, int width, int height);
}