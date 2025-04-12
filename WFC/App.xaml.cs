using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using WFC.Factories;
using WFC.Services;
using WFC.Services.Export;
using WFC.Services.System;
using WFC.ViewModels;
using Application = System.Windows.Application;

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
            // Регистрируем основные сервисы
            services.AddSingleton<IWFCService, DefaultWFCService>();
            services.AddSingleton<ITileFactory, DefaultTileFactory>();
        
            // Регистрируем новые сервисы для экспорта
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IVisualHelper, VisualHelper>();
            services.AddSingleton<IExporterFactory, ExporterFactory>();
            
            // Регистрируем ViewModel и главное окно

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