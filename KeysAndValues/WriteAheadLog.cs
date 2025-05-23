namespace KeysAndValues;

public class WriteAheadLog
{
    string fileName;

    public WriteAheadLog(string fileName)
    {
        this.fileName = fileName;
        Open(fileName);
    }

    private static Stream Open(string fileName)
    {
        var fi = new FileInfo(fileName);
        var fibak = new FileInfo($"{fileName}.bak");
        if (fibak.Exists)
        {
            return OpenFromBackup(fi, fibak);
        }

        var fs = fi.Open(FileMode.Append, FileAccess.ReadWrite, FileShare.Read);
        try
        {
            return fs;
        }
        catch (Exception)
        {
            throw;
        }
    }

    private static Stream OpenFromBackup(FileInfo fi, FileInfo fibak)
    {
        throw new NotImplementedException();
    }
}
