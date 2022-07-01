using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        Rules.Add(() => CheckDirectorySizeConstraint(), "The directory or its sub-directory(s) could not be accessed or they contain more than 100 files. (Maximum file count: 100)", nameof(SearchPath));
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

    TraceSource log = global::OpenTap.Log.CreateSource("Python");

    public int ReadWritePluginSearchPath(string addSearchPath, string remSearchPath, bool getList)
    {
        var currentPluginSearchPath = PythonSettings.Current.SearchPathList;

        if (getList)
        {
            if (currentPluginSearchPath.Count == 0)
                log.Info("The additional search path list is empty.\n");
            else
            {
                currentPluginSearchPath.ForEach(x =>
                {
                    string status = x.Enabled ? "Enabled" : "Disabled";
                    log.Info($"Path: '{x.SearchPath}', Status: {status}\n");
                });
            }
        }

        if (!string.IsNullOrEmpty(addSearchPath.Trim()))
        {
            var searchPaths = new List<string>(addSearchPath.Split(';')).Select(x => x.Trim()).ToList();
            searchPaths.RemoveAll(x => string.IsNullOrEmpty(x));
            foreach (var path in searchPaths)
            {
                string fullSearchPath = Path.GetFullPath(path);
                if (!Directory.Exists(fullSearchPath))
                {
                    log.Warning("Warning: The directory '{0}' does not exist.\n", fullSearchPath);
                }
                else if (currentPluginSearchPath.Exists(x => string.Compare(x.SearchPath, fullSearchPath) == 0))
                    log.Warning("Warning: '{0}' exists in the additional search path list.\n", fullSearchPath);
                else
                {
                    currentPluginSearchPath.Add(new PluginSearchPath() { SearchPath = fullSearchPath });
                    log.Info("Added' {0}' to the additional search path list.\n", fullSearchPath);
                }
            }
        }

        if (!string.IsNullOrEmpty(remSearchPath.Trim()))
        {
            var searchPaths = new List<string>(remSearchPath.Split(';')).Select(x => x.Trim()).ToList();
            searchPaths.RemoveAll(x => string.IsNullOrEmpty(x));
            foreach (var searchPath in searchPaths)
            {
                string fullSearchPath = Path.GetFullPath(searchPath);
                if (!Directory.Exists(fullSearchPath))
                {
                    log.Warning("Warning: The directory '{0}' does not exist.\n", fullSearchPath);
                }
                else if (currentPluginSearchPath.Remove(currentPluginSearchPath.Find(x => string.Compare(x.SearchPath, fullSearchPath) == 0)))
                    log.Info("Removed '{0}' from the additional search path list.\n", fullSearchPath);
                else
                {
                    log.Warning("Warning: The directory '{0}' is not in the additional search path list.\n", fullSearchPath);
                }
            }
        }
        PythonSettings.Current.Save();
        return 0;
    }
}