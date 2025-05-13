using System;
using System.IO;
using Unity.CodeEditor;
using System.Diagnostics;
using IOPath = System.IO.Path;
using Microsoft.Unity.VisualStudio.Editor; // Added for IVisualStudioInstallation and IGenerator
using System.Collections.Generic; // Added for IEnumerable in GetInstallations

namespace Unity.VoidEditor.Editor // Or your preferred namespace
{
    // We might not need a separate interface if we are directly implementing.
    // internal interface IVoidEditorInstallation 
    // {
    //     string Path { get; }
    //     bool SupportsAnalyzers { get; }
    //     Version LatestLanguageVersionSupported { get; }
    //     string[] GetAnalyzers();
    //     CodeEditor.Installation ToCodeEditorInstallation();
    //     bool Open(string path, int line, int column, string solutionPath); // solutionPath could be projectFolderPath for VS Code
    //     IGenerator ProjectGenerator { get; }
    //     void CreateExtraFiles(string projectDirectory);
    // }

    internal class VoidEditorInstallation : IVisualStudioInstallation // Potentially inherit from a base class if commonalities with other editors are identified later
    {
        public string Name { get; set; }
        public string Path { get; set; } // This might be just the command name like "code" or "voideditor"
        public Version Version { get; set; }
        public bool IsPrerelease { get; set; }

        public VoidEditorInstallation(string name, string path, Version version, bool isPrerelease = false)
        {
            Name = name;
            Path = path; // Should be the command to run Void Editor (e.g., "code" or a specific executable path)
            Version = version;
            IsPrerelease = isPrerelease;
        }

        public bool SupportsAnalyzers => true; // VS Code with C# extension typically supports Roslyn analyzers

        public Version LatestLanguageVersionSupported
        {
            get
            {
                // This needs to be determined based on Unity version and C# extension capabilities
                // For now, let's default to a common recent version, e.g., C# 9.0 or 10.0
                // This should align with what IGenerator (ProjectGeneration) will set in .csproj
                return new Version(9, 0); // Example, adjust as needed
            }
        }

        public string[] GetAnalyzers()
        {
            // For VS Code, analyzers are typically managed by the C# extension (OmniSharp)
            // or included via NuGet packages in the project.
            // We might not need to provide specific paths here unless Void Editor has a special way.
            return Array.Empty<string>();
        }

        public IGenerator ProjectGenerator
        {
            get
            {
                // Assuming SdkStyleProjectGeneration is suitable for VS Code
                // We might need to ensure this class is accessible or reimplement parts if not.
                // The namespace Microsoft.Unity.VisualStudio.Editor.ProjectGeneration might need adjustment
                // For now, let's assume we can instantiate it.
                // This will require the SdkStyleProjectGeneration.cs file to be part of the compilation.
                return new Microsoft.Unity.VisualStudio.Editor.SdkStyleProjectGeneration();
            }
        }

        public void CreateExtraFiles(string projectDirectory)
        {
            // Optional: Create .vscode/settings.json or .vscode/extensions.json
            // Example: recommend C# extension
            // var vscodeDir = IOPath.Combine(projectDirectory, ".vscode");
            // if (!Directory.Exists(vscodeDir))
            // {
            //    Directory.CreateDirectory(vscodeDir);
            // }
            // string extensionsJsonPath = IOPath.Combine(vscodeDir, "extensions.json");
            // if (!File.Exists(extensionsJsonPath))
            // {
            //    File.WriteAllText(extensionsJsonPath, "{\n  \"recommendations\": [\n    \"ms-dotnettools.csharp\"\n  ]\n}");
            // }
        }

        public bool Open(string filePath, int line, int column, string projectPath)
        {
            if (string.IsNullOrEmpty(Path)) // Path should be the command for Void Editor
            {
                UnityEngine.Debug.LogError("Void Editor path (command) is not set.");
                return false;
            }

            string arguments;
            if (line > 0)
            {
                // VS Code uses -g or --goto for file:line:column
                // Ensure filePath is quoted if it contains spaces
                arguments = $"--goto \"{filePath}\":{line}:{column}";
            }
            else
            {
                arguments = $"\"{filePath}\"";
            }
            
            // If a projectPath is provided and it's different from the file's directory,
            // VS Code usually opens the folder, and then the file within that context.
            // The primary argument to `code` should be the folder to open if it's a project context.
            // Then, --goto can specify the file.

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Path, // "code" or the specific command for Void Editor
                Arguments = arguments,
                UseShellExecute = true // Often better for launching GUI apps from a path/command
            };
            
            // If you want to open the project folder first, and then the file:
            if (!string.IsNullOrEmpty(projectPath) && Directory.Exists(projectPath))
            {
                 // First argument is the project folder, then --goto for the file
                 // Ensure projectPath is quoted
                startInfo.Arguments = $"\"{projectPath}\" {arguments}";
            }


            try
            {
                Process process = new Process { StartInfo = startInfo };
                process.Start();
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error opening Void Editor: {e.ToString()}");
                return false;
            }
        }

        public CodeEditor.Installation ToCodeEditorInstallation()
        {
            return new CodeEditor.Installation() { Name = Name, Path = Path };
        }

        // Static members for discovery
        private static string[] s_PotentialCommands = new[] { "voideditor", "code" }; // Add other potential command names for Void Editor
        private static string s_EditorCommand = null; // Stores the found command

        public static void Initialize()
        {
            // Attempt to find the correct command for Void Editor
            if (s_EditorCommand != null) return;

            foreach (var cmd in s_PotentialCommands)
            {
                try
                {
                    // Try running "command --version" to see if it exists and is responsive
                    Process process = new Process();
                    process.StartInfo.FileName = cmd;
                    process.StartInfo.Arguments = "--version";
                    process.StartInfo.UseShellExecute = false; // Set to false to allow redirection
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true; // Don't show a window

                    process.Start();
                    process.WaitForExit(2000); // Wait 2 seconds

                    if (process.ExitCode == 0)
                    {
                        s_EditorCommand = cmd;
                        UnityEngine.Debug.Log($"Found Void Editor command: {s_EditorCommand}");
                        break;
                    }
                }
                catch
                {
                    // Command not found or other error, try next
                }
            }

            if (s_EditorCommand == null)
            {
                UnityEngine.Debug.LogWarning("Void Editor command could not be found automatically. Users may need to specify the path manually.");
            }
        }


        public static System.Collections.Generic.IEnumerable<VoidEditorInstallation> GetInstallations()
        {
            Initialize(); // Ensure editor command is detected

            if (s_EditorCommand == null)
            {
                yield break; // No command found, so no installations to return
            }

            // Try to get version information
            Version version = new Version(0,0); // Default version if not determinable
            string displayName = "Void Editor"; // Default display name

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = s_EditorCommand;
                process.StartInfo.Arguments = "--version";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd(); // Read the version output
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    // Attempt to parse version from output. This is highly dependent on VS Code's --version format.
                    // Example VS Code output:
                    // 1.85.1
                    // 08ee524518e07c5096ASDBCAFDEBB9CC6DE59168DD
                    // arm64
                    // We'll take the first line as the version.
                    var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 0)
                    {
                        if (Version.TryParse(lines[0], out Version parsedVersion))
                        {
                            version = parsedVersion;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"Could not get Void Editor version: {e.Message}");
            }

            yield return new VoidEditorInstallation(displayName, s_EditorCommand, version);
        }

        public static bool TryDiscoverInstallation(string editorPath, out VoidEditorInstallation installation)
        {
            Initialize(); 

            installation = null;
            string commandToTest = editorPath;
            string displayName = "Void Editor";

            if (Array.Exists(s_PotentialCommands, cmd => cmd.Equals(editorPath, StringComparison.OrdinalIgnoreCase)) && s_EditorCommand != null)
            {
                commandToTest = s_EditorCommand;
            }
            else if (!IOPath.IsPathRooted(editorPath) && s_EditorCommand != null) // if editorPath is not a full path AND s_EditorCommand is found
            {
                commandToTest = s_EditorCommand; // Prefer the found command
            }
            // If editorPath is a full path, or if s_EditorCommand was not found, commandToTest remains editorPath

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = commandToTest; 
                process.StartInfo.Arguments = "--version";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    Version version = new Version(0,0);
                    var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 0)
                    {
                        if (Version.TryParse(lines[0], out Version parsedVersion))
                        {
                            version = parsedVersion;
                        }
                    }
                    
                    if (IOPath.IsPathRooted(editorPath)) // Use original editorPath for display name if it was a full path
                    {
                        displayName = $"Void Editor ({IOPath.GetFileNameWithoutExtension(editorPath)})";
                    }

                    installation = new VoidEditorInstallation(displayName, commandToTest, version); // Use commandToTest for actual execution
                    return true;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"Failed to verify Void Editor at {editorPath}: {e.Message}");
            }

            installation = null;
            return false;
        }
    }
}
