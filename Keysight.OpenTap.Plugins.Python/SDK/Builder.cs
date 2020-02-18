//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using OpenTap;
using OpenTapTraceSource = OpenTap;
namespace Keysight.OpenTap.Plugins.Python.SDK
{
    class Builder
    {
        public enum ExitCodes : int
        {
            Success = 0,
            UnableToBuildWrapper = 1,
            InvalidModuleName = 2,
            UnableToLoadPython = 3,
            ModuleNotFound = 4
        }
        static bool pythonModuleNameValidator(char character)
        {
            return char.IsLetterOrDigit(character) || character == '_';
        }

        public static OpenTapTraceSource.TraceSource log = Log.CreateSource("Python");
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ExitCodes doBuild(string modulename, bool include_pyc, bool build_tap_plugin, bool replace_package_xml, string dump_package_xml)
        {
            if (WrapperBuilder.LoadPython() == false)
                return ExitCodes.UnableToLoadPython;
            return doBuildint(modulename, include_pyc, build_tap_plugin, replace_package_xml, dump_package_xml);
        }


        public static ExitCodes doBuildint(string modulename, bool include_pyc, bool build_tap_plugin, bool replace_package_xml, string dump_package_xml)
        {
            log.Info("Start building {0}.", modulename);
            new PythonSettings();
            var pluginSearchPaths = PythonSettings.Current.GetSearchPaths().Distinct();
            var foundPluginPaths = new List<string>();
            var exitcode = ExitCodes.Success;
            string defaultModulePath = Path.Combine(Path.GetDirectoryName(typeof(Builder).Assembly.Location), modulename);
            string actualModulePath = "";

            foreach (var chr in modulename.Distinct())
            {
                if (pythonModuleNameValidator(chr) == false)
                {
                    log.Error("The module name cannot contain '{0}'. Char code: {1}", chr, (int)chr);
                    exitcode = ExitCodes.InvalidModuleName;
                }
            }

            if (char.IsDigit(modulename[0]))
            {
                log.Error("The module name must not start with a number.");
                exitcode = ExitCodes.InvalidModuleName;
            }

            foreach (var path in pluginSearchPaths)
            {
                if (!Directory.Exists(path))
                {
                    log.Error($"The plugin module search path is not found: '{path}'.");
                    exitcode = ExitCodes.ModuleNotFound;
                }
                else if (Directory.Exists(Path.Combine(path, modulename)))
                    foundPluginPaths.Add(Path.Combine(path, modulename));
            }

            foundPluginPaths = foundPluginPaths.Distinct().ToList();

            if (foundPluginPaths.Count == 0 && !Directory.Exists(defaultModulePath))
            {
                log.Error($"The plugin module is not found: '{modulename}'.");
                exitcode = ExitCodes.ModuleNotFound;
            }
            else if (foundPluginPaths.Count == 0 && Directory.Exists(defaultModulePath))
                actualModulePath = defaultModulePath;
            else
                actualModulePath = foundPluginPaths.First();

            if (exitcode != ExitCodes.Success)
            {
                // Ensure Python is in state that can be aborted.
                PyThread.Invoke(() => { });

                log.Info("Exiting...");
                Log.Flush();
                return exitcode;
            }

            try
            {
                var dllFileName = Path.Combine(actualModulePath, "Python." + modulename + ".dll");
                new WrapperBuilder().Build(new List<string> { modulename }, dllFileName, false, modulename);
                var dllLegacyFileName = Path.Combine(actualModulePath, modulename + ".dll");
                if (File.Exists(dllLegacyFileName)) // delete legacy dlls named without the 'Python.' prefix.
                    File.Delete(dllLegacyFileName);
            }
            catch(global::Python.Runtime.PythonException)
            {
                Log.Flush();
                return ExitCodes.UnableToBuildWrapper;
            }
            catch(BuildException b)
            {
                if (b.PrintErrors)
                {
                    log.Error("Unable to compile generated code.");
                    foreach (var error in b.Messages)
                    {
                        log.Info("{0}", error);
                    }
                }
                return ExitCodes.UnableToBuildWrapper;
            }
            catch (Exception ex)
            {
                log.Error("Caught exception while creating plugin.");
                log.Debug(ex);
                Log.Flush();
                return ExitCodes.UnableToBuildWrapper;
            }

            bool pyconly = include_pyc;

            if (build_tap_plugin)
            {
                try
                {
                    Assembly.Load("OpenTap.Package");
                }
                catch
                {
                    log.Error("Cannot build the TapPackage file.");
                    log.Info("Unable to load tap.exe. Is the TAP SDK installed?");
                    log.Info("To install the TAP SDK, please re-install TAP.");
                    log.Flush();
                    return ExitCodes.UnableToLoadPython;
                }
                buildTapPackage(modulename, pyconly, replace_package_xml, dump_package_xml);
            }
            log.Info("{0} Completed.", modulename);
            return exitcode;    
        }

        // buildTapPlugin needs to be encapsulated, because it depends on Keysight.Tap.Package
        // if the exe is missing, any method using it will throw an exception.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void buildTapPackage(string modulename, bool pyconly, bool replace_package_xml, string dump_package_xml)
        {
            var modpath = Path.Combine(Path.GetDirectoryName(typeof(Builder).Assembly.Location), modulename);
            List<string> files = new List<string>();
            var allfiles = Directory.EnumerateFiles(modpath, "*", SearchOption.AllDirectories);
            foreach (var file in allfiles)
            {
                var ext = Path.GetExtension(file);
                if (ext == ".py" && pyconly)
                {
                    continue;
                }
                else if (ext == ".pyc" && !pyconly)
                {
                    continue;
                }
                files.Add(file);
            }
            string defaultPackageName = "package.xml";
            if (replace_package_xml || !File.Exists(Path.Combine(modpath, "package.xml")) || dump_package_xml != null)
            {
                var xdoc = new XDocument();
                XNamespace aw = @"http://opentap.io/schemas/package";
                XElement package_elem = null;
                XElement files_elem = new XElement(aw + "Files");
                XElement description = new XElement(aw + "Description");
                description.Value = "Add a description here";
                xdoc.Add(package_elem = new XElement(aw + "Package"));
                package_elem.Add(files_elem);
                package_elem.Add(description);

                package_elem.SetAttributeValue("InfoLink", "");
                package_elem.SetAttributeValue("Version", "1.0.0");
                
                package_elem.SetAttributeValue("Name", modulename);
                foreach (var file in files)
                {
                    if (file.EndsWith(defaultPackageName))
                        continue;
                    var fileelem = new XElement(aw + "File");
                    fileelem.SetAttributeValue("Path", file);
                    files_elem.Add(fileelem);
                }
                var targetxml = Path.Combine(modpath, defaultPackageName);
                if (string.IsNullOrWhiteSpace(dump_package_xml) == false)
                {
                    targetxml = Path.Combine(Path.GetDirectoryName(typeof(Builder).Assembly.Location), dump_package_xml);
                }
                File.Delete(targetxml);
                using (var f = File.Open(targetxml, FileMode.Create, FileAccess.ReadWrite))
                {
                    using (var writer = XmlWriter.Create(f, new XmlWriterSettings() { Indent = true }))
                    {
                        xdoc.WriteTo(writer);
                    }
                }
                Console.WriteLine("Created file: '{0}'.", targetxml);
                if (string.IsNullOrWhiteSpace(dump_package_xml) == false)
                    return;
            }

            createPackage(Path.Combine(modpath, defaultPackageName), modulename + ".TapPackage");
            log.Info("Saved to '{0}'.", Path.Combine(Directory.GetCurrentDirectory(), modpath, modulename + ".TapPackage"));
            log.Flush();
        }

        private static void createPackage(string inputXmlPath, string outputPath)
        {
            //generating package by using command line
            string destPluginPackageFile = Path.Combine(Directory.GetCurrentDirectory(), Path.GetDirectoryName(inputXmlPath), outputPath);

            string packageManagerFile = PyThread.IsWin32 ? Path.Combine(Directory.GetCurrentDirectory(), "tap.exe")
                : Path.Combine(Directory.GetCurrentDirectory(), "tap");
            string sourceXMLPath = Path.Combine(Directory.GetCurrentDirectory(), inputXmlPath);
            string argument = "package create " + "\"" + sourceXMLPath + "\""
                + " -o \"" + destPluginPackageFile + "\"";
            string error = "", output = "";
            int exitcode = 0;
            using (Process p = new Process())
            {
                p.StartInfo.FileName = packageManagerFile;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.Arguments = argument;
                p.Start();

                //get the output infomation
                output = p.StandardOutput.ReadToEnd();
                error = p.StandardError.ReadToEnd();
                p.WaitForExit();
                exitcode = p.ExitCode;
                p.Close();
            }
            if (exitcode == 0)
            {
                log.Info("Successfully generated package.");
            }
            else
            {
                log.Error(string.Format("Failed generating package with exit code {0}:{1} ", exitcode, error));
            }
        }
    }
}
