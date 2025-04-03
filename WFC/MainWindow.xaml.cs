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