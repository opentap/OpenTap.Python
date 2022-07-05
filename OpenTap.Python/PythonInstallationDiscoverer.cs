using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenTap.Python;

class PythonDiscoverer
{
    public static PythonDiscoverer Instance { get; } = new PythonDiscoverer();
    public IEnumerable<string> AvailablePythonLibraries => GetAvailablePythonInstallations().Select(x => x.library);

    public IEnumerable<string> GetAvailablePythonVersions() => GetAvailablePythonVersionsCandidates().Distinct();
    IEnumerable<string> GetAvailablePythonVersionsCandidates()
    {
        foreach (var ins in GetAvailablePythonInstallations())
        {
            if (pythonVersionParser.IsMatch(ins.library))
                yield return $"3.{pythonVersionParser.Match(ins.library).Groups["v"].Value}";
        }
    }
    
    public IEnumerable<(string library, string pyPath)> GetAvailablePythonInstallations()
    {
        return GetAvailablePythonInstallationCandidates()
            .Where(x => x.lib != null && File.Exists(x.lib))
            .OrderByDescending(x => x.weight)
            .Select(x => (x.lib, x.pyPath));
    }
    static IEnumerable<string> GetPythonsInFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return Enumerable.Empty<string>();
        }
        List<string> pys = new List<string>();
        foreach(var dir in Directory.GetDirectories(folderPath))
        {
            if (Path.GetFileName(dir).StartsWith("Python", StringComparison.CurrentCultureIgnoreCase))
            {
                pys.Add(dir);
            }
        }
        return pys;

    }
    static IEnumerable<string> LocatePythons()
    {
        if (!SharedLib.IsWin32)
            return Array.Empty<string>();
        var drives = Directory.GetLogicalDrives();
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
        var programFiles2 = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
        var programFiles3 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var programFiles4 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFiles5 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "Python");

        return drives.Concat(new[] { programFiles, programFiles2, programFiles3, programFiles4, programFiles5 })
            .SelectMany(GetPythonsInFolder).Distinct().ToArray();
    }

    public IEnumerable<string> PyFolders => LocatePythons();

    IEnumerable<(string lib, string pyPath, int weight)> GetAvailablePythonInstallationCandidates()
    {
        if (PythonSettings.Current.PythonLibraryPath != null && File.Exists(PythonSettings.Current.PythonLibraryPath))
        {
            yield return (PythonSettings.Current.PythonLibraryPath, PythonSettings.Current.PythonPath, 1000);
        }

        if (Environment.GetEnvironmentVariable("PYTHONHOME") is string home && Directory.Exists(home))
        {
            var pyInHome = TryFindPythons(home).FirstOrDefault();
            if (File.Exists(pyInHome))
                yield return (pyInHome, home, 0);
        }
        
        {
            string pyPath = PythonSettings.Current.PythonPath;
            if (string.IsNullOrWhiteSpace(pyPath) == false)
                pyPath = Path.GetFullPath(pyPath);
            if (Utils.RobustDirectoryExists(pyPath))
            {
                var path = new DirectoryInfo(pyPath);
                var loc = TryFindPythons(path.FullName).FirstOrDefault();
                if (loc != null && pythonVersionParser.IsMatch(loc))
                {
                    Int32.TryParse(pythonVersionParser.Match(loc).Groups["v"].Value, out int weight);
                    yield return (loc, pyPath, weight);
                }
            }
        }
        if (SharedLib.IsWin32 == false)
        {
            foreach (var python in TryFindPythons("/usr/lib/x86_64-linux-gnu/"))
            {
                Int32.TryParse(pythonVersionParser.Match(python)?.Groups["v"].Value, out int weight);
                if(python != null)
                    yield return (python, null, weight);
            }
        }
    }

    static readonly Regex pythonVersionParser = new ("(libpython3|python3)\\.?(?<v>[0-9]+)\\.(?<ext>so|dll|dylib)", RegexOptions.Compiled); 
    
    static IEnumerable<string> TryFindPythons(string path)
    {
        
        if (false == string.IsNullOrWhiteSpace(path))
        {
            foreach (string subPath in new[] {"", "Scripts"})
            {
                var subPath2 = Path.Combine(path, subPath);
                if (Directory.Exists(subPath2))
                {
                    IEnumerable<string> files;
                    try
                    {
                        files = Directory.EnumerateFiles(subPath2).Where(p => pythonVersionParser.IsMatch(p));
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var subPath3 in files)
                    {
                        if (SharedLib.IsWin32 == false)
                        {
                            if(subPath3.EndsWith(".so") || subPath3.Contains(".so."))
                            {
                                
                            }
                            else
                            { 
                                continue; // not a shared object file.
                            }
                            
                        }

                        var sharedLib = SharedLib.Load(subPath3);
                        if (sharedLib != null)
                        {
                            sharedLib.Unload();
                            yield return subPath3;
                        }
                    }
                }
            }
        }
    }
}