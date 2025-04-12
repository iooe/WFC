namespace WFC.Services.Export;

// Фабрика для создания экспортеров (паттерн Фабричный метод)
public interface IExporterFactory
{
    IExporter CreateExporter(ExportType type);
}