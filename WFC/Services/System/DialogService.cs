namespace WFC.Services.System;

public class DialogService : IDialogService
{
    public string ShowSaveFileDialog(string title, string filter, string defaultExt, string defaultFilename)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = title,
            Filter = filter,
            DefaultExt = defaultExt,
            FileName = defaultFilename
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string ShowFolderBrowserDialog(string description)
    {
        var dialog = new FolderBrowserDialog
        {
            Description = description,
            UseDescriptionForTitle = true
        };

        return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
    }
}