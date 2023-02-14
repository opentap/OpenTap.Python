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
        try
        {
            if (!Directory.Exists(folderPath))
            {
                return Enumerable.Empty<string>();
            }

            List<string> pys = new List<string>();
            foreach (var dir in Directory.GetDirectories(folderPath))
            {
                if (Path.GetFileName(dir).Contains("Python"))
                {
                    pys.Add(dir);
                }
            }

            return pys;
        }
        catch
        {
            return Array.Empty<string>();
        }

    }
    static IEnumerable<string> LocatePythonsWin32()
    {
        if (!SharedLib.IsWin32)
            return Array.Empty<string>();
        var drives = Directory.GetLogicalDrives();
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
        var programFiles2 = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
        var programFiles3 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var programFiles4 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");
        var programFiles5 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "Python");
        var programFiles6 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        return drives.Concat(new[] { programFiles, programFiles6, programFiles2, programFiles3, programFiles4, programFiles5 })
            .SelectMany(GetPythonsInFolder).Distinct().ToArray();
    }

    IEnumerable<(string lib, string pyPath, int weight)> GetAvailablePythonInstallationCandidates()
    {
        if (PythonSettings.Current.PythonLibraryPath != null && File.Exists(PythonSettings.Current.PythonLibraryPath))
        {
            yield return (PythonSettings.Current.PythonLibraryPath, PythonSettings.Current.PythonPath, 1000);
        }

        //pythonLocation  for github builds.
        if ((Environment.GetEnvironmentVariable("PYTHONHOME") ?? Environment.GetEnvironmentVariable("pythonLocation")) is string home && Directory.Exists(home))
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
        if (SharedLib.IsWin32)
        {
            foreach (var targetFolder in LocatePythonsWin32())
            {
                var loc = TryFindPythons(targetFolder).FirstOrDefault();
                if (loc != null && pythonVersionParser.IsMatch(loc))
                {
                    // if the target file folder contains a Lib folder,
                    // then it can probably be used as a PythonHome.
                    var fld = Path.GetDirectoryName(loc);
                    if (fld != null && !Directory.Exists(Path.Combine(fld, "Lib")))
                        fld = null;
                    Int32.TryParse(pythonVersionParser.Match(loc).Groups["v"].Value, out int weight);
                    yield return (loc, fld, weight);
                }
            }
        }
        else if (SharedLib.IsMacOs)
        {
            var homebrew = "/opt/homebrew/Frameworks/Python.framework/Versions/";
            var libraryFrameworks = "/Library/Frameworks/Python.framework/Versions/";
            foreach(var dir in new []{homebrew, libraryFrameworks}){
                if (Directory.Exists(dir))
                {
                    var subdirs = System.IO.Directory.EnumerateDirectories(dir, "3.*");
                    foreach (var subdir in subdirs)
                    {
                        var subdir2 = Path.Combine(subdir, "lib");
                        foreach (var python in TryFindPythons(subdir2))
                        {
                            Int32.TryParse(pythonVersionParser.Match(python)?.Groups["v"].Value, out int weight);
                            if (python != null)
                                yield return (python, null, weight);
                        }
                    }
                }
            }

        }
        else
        {
            foreach(var basePath in new [] {"/usr/lib/x86_64-linux-gnu/", "/usr/lib/aarch64-linux-gnu/"}
                        .Where(Directory.Exists))
            foreach (var python in TryFindPythons(basePath))
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
                        if (SharedLib.IsMacOs)
                        {
                            yield return subPath3;
                            continue;
                        }
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

                        if (SharedLib.Load(subPath3) is SharedLib slib)
                        {
                            slib.Unload();
                            yield return subPath3;
                        }

                    }
                }
            }
        }
    }
}