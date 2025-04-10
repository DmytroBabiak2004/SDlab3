namespace SDlab3.Services;

public class LoggerService
{
    private const string LogDirectoryPath = "Logs";

    public static void RecordTransferCount(int count, string model)
    {
        var logFilePath = GetLogFilePath(model, "number");

        if (!Directory.Exists(LogDirectoryPath))
        {
            Directory.CreateDirectory(LogDirectoryPath);
        }

        using var streamWriter = new StreamWriter(logFilePath, false);
        streamWriter.WriteLine(count);
    }

    public static int GetProcessedCount(string model)
    {
        var logFilePath = GetLogFilePath(model, "number");

        if (!File.Exists(logFilePath))
        {
            return 0;
        }

        using var streamReader = new StreamReader(logFilePath);
        return int.Parse(streamReader.ReadLine());
    }

    private static string GetLogFilePath(string model, string type)
    {
        return Path.Combine(LogDirectoryPath, $"{model}-{type}.txt");
    }
}