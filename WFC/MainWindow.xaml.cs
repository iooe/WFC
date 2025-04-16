using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WFC.ViewModels;
using CheckBox = System.Windows.Forms.CheckBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace WFC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Fields for panning implementation
        private Point _lastMousePosition;
        private bool _isPanning;

// Mouse wheel handler with zoom-to-cursor functionality
        private void MapScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null) return;

            var viewModel = DataContext as MainViewModel;
            if (viewModel == null) return;

            // Get current mouse position relative to the ScrollViewer
            Point mousePos = e.GetPosition(scrollViewer);

            // Get current scroll position
            double oldHorizontalOffset = scrollViewer.HorizontalOffset;
            double oldVerticalOffset = scrollViewer.VerticalOffset;

            // Get old and new zoom levels
            double oldZoom = viewModel.ZoomLevel;
            double newZoom = e.Delta > 0
                ? Math.Min(oldZoom + 0.1, 5.0) // Increase zoom (max 500%)
                : Math.Max(oldZoom - 0.1, 0.1); // Decrease zoom (min 10%)

            if (Math.Abs(newZoom - oldZoom) < 0.01) return;

            // Update zoom level
            viewModel.ZoomLevel = newZoom;

            // Calculate new scroll position for zoom-to-cursor
            if (scrollViewer.ScrollableWidth > 0 || scrollViewer.ScrollableHeight > 0)
            {
                // Calculate new scroll for zoom-to-cursor
                double scaleChange = newZoom / oldZoom;

                double newHorizontalOffset = (mousePos.X + oldHorizontalOffset) * scaleChange - mousePos.X;
                double newVerticalOffset = (mousePos.Y + oldVerticalOffset) * scaleChange - mousePos.Y;

                // Apply new scroll position
                scrollViewer.ScrollToHorizontalOffset(newHorizontalOffset);
                scrollViewer.ScrollToVerticalOffset(newVerticalOffset);
            }
        }

// Event handlers for map panning
        private void MapScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Start panning when right mouse button is pressed
            if (e.RightButton == MouseButtonState.Pressed)
            {
                var scrollViewer = sender as ScrollViewer;
                if (scrollViewer != null)
                {
                    _lastMousePosition = e.GetPosition(scrollViewer);
                    _isPanning = true;
                    scrollViewer.CaptureMouse();
                    e.Handled = true;
                }
            }
        }

        private void MapScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // Pan the map while dragging
            if (_isPanning)
            {
                var scrollViewer = sender as ScrollViewer;
                if (scrollViewer != null)
                {
                    Point currentPosition = e.GetPosition(scrollViewer);
                    double deltaX = currentPosition.X - _lastMousePosition.X;
                    double deltaY = currentPosition.Y - _lastMousePosition.Y;

                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - deltaX);
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - deltaY);

                    _lastMousePosition = currentPosition;
                    e.Handled = true;
                }
            }
        }

        private void MapScrollViewer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // End panning operation
            if (_isPanning)
            {
                var scrollViewer = sender as ScrollViewer;
                if (scrollViewer != null)
                {
                    scrollViewer.ReleaseMouseCapture();
                    _isPanning = false;
                    e.Handled = true;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

// Add this method to MainWindow.xaml.cs
        public UIElement GetTileContainer()
        {
            // Return the Canvas that contains all the tiles
            // You may need to adjust this depending on your exact XAML structure
            return TilesCanvas;
        }

        // Constructor that accepts a view model
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // This event is wired up but we don't need to do anything here
            // The Viewbox will automatically scale the Canvas based on available space
        }

        private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Логируем для отладки
            Console.WriteLine("CheckBox_CheckedChanged вызван");

            // Получаем CheckBox, который вызвал событие
            var checkBox = sender as CheckBox;
            if (checkBox == null)
            {
                Console.WriteLine("CheckBox равен null");
                return;
            }

            // Получаем контекст данных (PluginViewModel)
            var pluginVM = checkBox.DataContext as PluginViewModel;
            if (pluginVM == null)
            {
                Console.WriteLine("PluginViewModel равен null");
                return;
            }

            Console.WriteLine($"Плагин: {pluginVM.Name}, новое состояние: {pluginVM.Enabled}");

            // Получаем ViewModel окна
            var viewModel = DataContext as MainViewModel;
            if (viewModel == null)
            {
                Console.WriteLine("MainViewModel равен null");
                return;
            }

            // Вызываем команду
            if (viewModel.TogglePluginCommand.CanExecute(pluginVM))
            {
                Console.WriteLine("Выполняем команду TogglePluginCommand");
                viewModel.TogglePluginCommand.Execute(pluginVM);
            }
            else
            {
                Console.WriteLine("TogglePluginCommand недоступна для выполнения");
            }
        }
    }
}