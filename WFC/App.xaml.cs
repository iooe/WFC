using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using WFC.Factories;
using WFC.Factories.Model;
using WFC.Services;
using WFC.Services.Export;
using WFC.Services.System;
using WFC.ViewModels;
using WFC.Plugins;
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
            
            // Create required directories
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            Directory.CreateDirectory(Path.Combine(baseDir, "Models"));
            Directory.CreateDirectory(Path.Combine(baseDir, "TrainingData"));
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register core services
            services.AddSingleton<ITileFactory, TileFactory>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IVisualHelper, VisualHelper>();
            
            // Register plugin system
            services.AddSingleton<PluginManager>();
            services.AddSingleton<TileConfigManager>();
            
            // Register export services
            services.AddSingleton<IExporterFactory, ExporterFactory>();
            
            // Register WFC service
            services.AddSingleton<IWFCService, DefaultWFCService>();
            
            // Register ML
            services.AddSingleton<IModelFactory, DefaultModelFactory>();
            
            // Register view models and views
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