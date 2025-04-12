namespace WFC.Services;

public interface IFileSystem
{
    void CreateDirectory(string path);
    bool DirectoryExists(string path);
    void SaveToFile(byte[] data, string filePath);
}