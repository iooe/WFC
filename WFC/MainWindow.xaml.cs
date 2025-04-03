using System.Windows;
using WFC.ViewModels;

namespace WFC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
// Add this method to MainWindow.xaml.cs
        public UIElement GetTileContainer()
        {
            // Return the Canvas that contains all the tiles
            // You may need to adjust this depending on your exact XAML structure
            return GridContainer; // This is the Border that contains the Viewbox
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
    }
}