using System;
using System.Collections.Generic;
using System.IO;
namespace OpenTap.Python;
public class PluginSearchPath : ValidatingObject
{
    const int MAX_ENTRY_COUNT = 100;
    string searchPath;

    [Display("Search Path", "A search path for finding the Python based plugin modules.", Order:1)]
    [DirectoryPath]
    public string SearchPath
    {
        get => searchPath;
        set => searchPath = Path.GetFullPath(value);
    }

    [Display("Enabled", "Enable or disable the search path.", Order:0)]
    public bool Enabled { get; set; } = true;

    public PluginSearchPath()
    {
        Rules.Add(() =>
        {
            if(!string.IsNullOrEmpty(SearchPath))
                return Directory.Exists(Path.GetFullPath(SearchPath));
            return false;
        }, "This search path does not exist.", nameof(SearchPath));

        Rules.Add(CheckDirectorySizeConstraint, "The directory or its sub-directory(s) could not be accessed or they contain more than 100 files. (Maximum file count: 100)", nameof(SearchPath));
    }

    internal bool CheckDirectorySizeConstraint()
    {
        // check the existence of the search path first
        if (!string.IsNullOrWhiteSpace(SearchPath) && Directory.Exists(Path.GetFullPath(SearchPath)))
        {
            try
            {
                var startingDir = new DirectoryInfo(SearchPath);
                Queue<DirectoryInfo> dirQueue = new Queue<DirectoryInfo>();

                dirQueue.Enqueue(startingDir);

                int totalFileSystemEntryCount = 0;
                while (dirQueue.Count != 0)
                {
                    DirectoryInfo currentDir = dirQueue.Dequeue();
                    if (currentDir.Name == "bin" || currentDir.Name == "obj" || File.Exists(Path.Combine(currentDir.FullName, "__init__.py")))
                        continue;

                    // count this dir
                    totalFileSystemEntryCount++;

                    // count the files inside the dir
                    totalFileSystemEntryCount += currentDir.GetFiles().Length;
                        
                    if (totalFileSystemEntryCount > MAX_ENTRY_COUNT)
                        return false;
                    else
                    {
                        foreach (DirectoryInfo subDir in currentDir.GetDirectories())
                        {
                            dirQueue.Enqueue(subDir);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return false;
            }
        }
        return true;
    }

    static readonly TraceSource log = Log.CreateSource("Python");
}