namespace WFC.Services.Export;

public class ExporterFactory : IExporterFactory
{
    private readonly IDialogService _dialogService;
    private readonly IFileSystem _fileSystem;
    private readonly IVisualHelper _visualHelper;

    public ExporterFactory(IDialogService dialogService, IFileSystem fileSystem, IVisualHelper visualHelper)
    {
        _dialogService = dialogService;
        _fileSystem = fileSystem;
        _visualHelper = visualHelper;
    }

    public IExporter CreateExporter(ExportType type)
    {
        return type switch
        {
            ExportType.Png => new PngExporter(_dialogService, _visualHelper),
            ExportType.Tiles => new TilesExporter(_dialogService, _fileSystem, _visualHelper),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
}