using System.Diagnostics;
using System.IO;
using UnityEngine;
using System.Text;

public static class P4
{
    public static string P4Port { get; set; }
    public static string P4User { get; set; }

    public static bool IsMockMode = false;
    public static string MockLogPath;

    public static void SetMockMode(bool isMock)
    {
        IsMockMode = isMock;
        LogMockEvent($"Mock Mode {(isMock ? "Enabled" : "Disabled")}");
    }

    public static void LogMockEvent(string message)
    {
        if (string.IsNullOrEmpty(MockLogPath))
        {
            MockLogPath = Path.Combine(Application.persistentDataPath, "p4_mock_log.txt");
        }
        try { File.AppendAllText(MockLogPath, $"\n{message}\n"); } catch {}
    }

    public static (string Output, string Error) RunCommand(string arguments, string input = null)
    {
        if (string.IsNullOrEmpty(MockLogPath))
            MockLogPath = Path.Combine(Application.persistentDataPath, "p4_mock_log.txt");

        string logEntry = $"Command: p4 {arguments}\n";
        if (input != null) logEntry += $"Input Data:\n{input}\n";
        logEntry += "--------------------\n";

        try { File.AppendAllText(MockLogPath, logEntry); }
        catch (System.Exception e) { UnityEngine.Debug.LogError("Log Write Error: " + e.Message); }

        if (IsMockMode)
        {
            bool isReadOnly = arguments.Contains("group -o") || arguments.Contains("users") || arguments.Contains("login");
            
            if (!isReadOnly)
            {
                return ("Mock Command Success (Logged Only)", "");
            }
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "p4",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = (input != null),
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (!string.IsNullOrEmpty(P4Port)) startInfo.EnvironmentVariables["P4PORT"] = P4Port;
        if (!string.IsNullOrEmpty(P4User)) startInfo.EnvironmentVariables["P4USER"] = P4User;

        using (var process = Process.Start(startInfo))
        {
            if (input != null)
            {
                process.StandardInput.Write(input);
                process.StandardInput.Close();
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(output)) UnityEngine.Debug.Log($"P4 Output: {output}");
            if (!string.IsNullOrEmpty(error)) UnityEngine.Debug.LogError($"P4 Error: {error}");

            return (output, error);
        }
    }
}