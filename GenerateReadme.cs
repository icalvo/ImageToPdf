using System.Diagnostics;
File.WriteAllText(
    "README.md",
    File.ReadAllText("README.md.template")
        .Replace(
            "[[HELP]]",
            RunCapture("dotnet", "run ImageToPdf.cs -- -h")));

// Runs a command and returns its captured standard output.
static string RunCapture(string fileName, string arguments)
{
    try
    {
        using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });
        if (process is null)
            return "";
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  ! Failed to run '{fileName} {arguments}': {ex.Message}");
        return "";
    }
}
