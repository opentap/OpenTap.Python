using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

    const int minSupportedMinorVersion = 7;
    const int maxSupportedMinorVersion = 13;
    const int supportedMajorVersion = 3;
    
    public IEnumerable<(string library, string pyPath)> GetAvailablePythonInstallations()
    {
        var installations = ValidateInstallations(GetAvailablePythonInstallationCandidates());
        return SharedLib.IsWin32
            ? GetInstallationsWithFallback(installations,
                () => GetInstallationsWithFallback(
                    ValidateInstallations(GetWindowsCandidates(GetWindowsPathFolders())),
                    () => ValidateInstallations(GetWindowsCandidates(GetFixedDrivePythonFolders()))))
            : installations;
    }

    internal static IEnumerable<(string library, string pyPath)> GetInstallationsWithFallback(
        IEnumerable<(string library, string pyPath)> preferred,
        Func<IEnumerable<(string library, string pyPath)>> fallback)
    {
        var installations = preferred.ToArray();
        return installations.Length > 0 ? installations : fallback().ToArray();
    }

    static IEnumerable<(string library, string pyPath)> ValidateInstallations(
        IEnumerable<(string lib, string pyPath, int weight)> candidates)
    {
        return candidates
            .Where(x => x.lib != null && File.Exists(x.lib))
            .OrderByDescending(x => x.weight)
            .ToArray()
            // only specific python version are supported. This is detected by trying to load the dll and get the python version from it.
            // this does not work on MacOS, so there we just assume that its ok.
            .Where(x => SharedLib.IsMacOs || GetVersion(x.lib, out int major, out int minor) && minor <= maxSupportedMinorVersion && minor >= minSupportedMinorVersion && major == supportedMajorVersion)
            .Select(x => (x.lib, x.pyPath));
    }

    delegate IntPtr IntPtrF();

    
    static bool GetVersion(string libPath, out int major, out int minor)
    {
        // try parsing the version from the python function stymbols.
        major = 0;
        minor = 0;
        
        var sh = SharedLib.Load(libPath);

        if (sh is null)
            return false;

        var versionSymbol = sh.GetSymbol("Py_GetVersion");
        if (versionSymbol == IntPtr.Zero) 
            return false;
        
        var versionFunc = Marshal.GetDelegateForFunctionPointer<IntPtrF>(versionSymbol);
        var versionStringPtr = versionFunc();
        if (versionStringPtr == IntPtr.Zero)
            return false;
        
        var versionString = Marshal.PtrToStringAnsi(versionStringPtr);
        
        return int.TryParse(versionString.Split('.').ElementAtOrDefault(0) ?? "", out major) && int.TryParse(versionString.Split('.').ElementAtOrDefault(1) ?? "", out minor);
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
                string fileName = Path.GetFileName(dir);
                if (fileName.ToLower().Contains("python"))
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
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
        var programFiles2 = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
        var programFiles3 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var programFiles4 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");
        var programFiles5 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "Python");
        var programFiles6 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFiles7 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Python");

        // Check the usual installation folders before PATH or drive-root discovery.
        return new[] { programFiles5, programFiles6, programFiles3, programFiles7, programFiles4, programFiles, programFiles2 }
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .SelectMany(GetPythonsInFolder)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    static IEnumerable<string> GetWindowsPathFolders()
    {
        return (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator)
            .Select(path => path.Trim().Trim('"'))
            .Where(path => !string.IsNullOrWhiteSpace(path) && Path.IsPathRooted(path))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    static IEnumerable<string> GetFixedDrivePythonFolders()
    {
        // Do not touch mapped network drives, even when the normal discovery methods fail.
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType == DriveType.Fixed)
            {
                foreach (var folder in GetPythonsInFolder(drive.Name))
                    yield return folder;
            }
        }
    }

    static IEnumerable<(string lib, string pyPath, int weight)> GetWindowsCandidates(IEnumerable<string> folders)
    {
        foreach (var targetFolder in folders)
        {
            var loc = TryFindPythons(targetFolder).FirstOrDefault();
            if (loc != null && pythonVersionParser.IsMatch(loc))
            {
                // A Lib folder is required for the installation to serve as PythonHome.
                var fld = Path.GetDirectoryName(loc);
                if (fld != null && !Directory.Exists(Path.Combine(fld, "Lib")))
                    fld = null;
                Int32.TryParse(pythonVersionParser.Match(loc).Groups["v"].Value, out int weight);
                yield return (loc, fld, weight);
            }
        }
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
            foreach (var candidate in GetWindowsCandidates(LocatePythonsWin32()))
                yield return candidate;
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
        else // Assume OS is Linux
        {
            foreach(var basePath in new [] {
                            "/usr/lib/x86_64-linux-gnu/", 
                            "/usr/lib/aarch64-linux-gnu/", 
                            "/usr/lib/" // alpine linux
                            
                        }
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
