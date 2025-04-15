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