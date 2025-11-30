using System.Diagnostics;

public static class P4
{
    public static string P4Port { get; set; }
    public static string P4User { get; set; }

    public static (string Output, string Error) RunCommand(string arguments, string input = null)
    {
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

        if (!string.IsNullOrEmpty(P4Port))
        {
            startInfo.EnvironmentVariables["P4PORT"] = P4Port;
        }
        if (!string.IsNullOrEmpty(P4User))
        {
            startInfo.EnvironmentVariables["P4USER"] = P4User;
        }

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

            if (!string.IsNullOrEmpty(output))
            {
                UnityEngine.Debug.Log($"P4 Output: {output}");
            }
            if (!string.IsNullOrEmpty(error))
            {
                UnityEngine.Debug.LogError($"P4 Error: {error}");
            }

            return (output, error);
        }
    }
}