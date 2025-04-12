using System.IO;

namespace WFC.Services.System;

public class FileSystem : IFileSystem
{
    public void CreateDirectory(string path)
    {
        if (!DirectoryExists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public void SaveToFile(byte[] data, string filePath)
    {
        File.WriteAllBytes(filePath, data);
    }
}