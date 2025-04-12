namespace WFC.Services;

public interface IDialogService
{
    string ShowSaveFileDialog(string title, string filter, string defaultExt, string defaultFilename);
    string ShowFolderBrowserDialog(string description);
}
