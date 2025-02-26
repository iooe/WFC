using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using WFC.Services;
using WFC.ViewModels;

namespace WFC
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IWFCService, DefaultWFCService>();
            services.AddSingleton<MainViewModel>();
            services.AddTransient<MainWindow>();
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}