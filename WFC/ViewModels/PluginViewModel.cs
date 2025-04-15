using System.ComponentModel;
using WFC.Plugins;

namespace WFC.ViewModels;

/// <summary>
/// View model for a plugin
/// </summary>
public class PluginViewModel : INotifyPropertyChanged
{
    private readonly IPlugin _plugin;
    
    public string Id => _plugin.Id;
    public string Name => _plugin.Name;
    public string Version => _plugin.Version;
    public string Description => _plugin.Description;
    
    private bool _enabled;
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                _plugin.Enabled = value;
                OnPropertyChanged(nameof(Enabled));
            }
        }
    }
    
    public string PluginType
    {
        get
        {
            if (_plugin is ITileSetPlugin)
                return "Tile Set";
            if (_plugin is IGenerationHookPlugin && _plugin is IPostProcessorPlugin)
                return "Generation + Post-processing";
            if (_plugin is IGenerationHookPlugin)
                return "Generation Hook";
            if (_plugin is IPostProcessorPlugin)
                return "Post-processor";
            return "Unknown";
        }
    }
    
    public PluginViewModel(IPlugin plugin)
    {
        _plugin = plugin;
        _enabled = plugin.Enabled;
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}