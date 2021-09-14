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
using System.Threading.Tasks;
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
        public static ExitCodes doBuild(string modulename, bool include_pyc, bool build_tap_plugin, bool replace_package_xml, string xml_file_name)
        {
            if (WrapperBuilder.LoadPython() == false)
                return ExitCodes.UnableToLoadPython;
            return doBuildint(modulename, include_pyc, build_tap_plugin, replace_package_xml, xml_file_name);
        }


        public static ExitCodes doBuildint(string modulename, bool include_pyc, bool build_tap_plugin, bool replace_package_xml, string xml_file_name)
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
            catch (global::Python.Runtime.PythonException)
            {
                Log.Flush();
                return ExitCodes.UnableToBuildWrapper;
            }
            catch (BuildException b)
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
                buildTapPackage(actualModulePath, pyconly, replace_package_xml, xml_file_name);
            }
            log.Info("{0} Completed.", modulename);
            return exitcode;
        }

        // buildTapPlugin needs to be encapsulated, because it depends on Keysight.Tap.Package
        // if the exe is missing, any method using it will throw an exception.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void buildTapPackage(string sourcePath, bool pyconly, bool replace_package_xml, string xml_file_name)
        {

            string pluginName = new DirectoryInfo(sourcePath).Name;
            string opentapPath = Path.GetDirectoryName(PluginManager.GetOpenTapAssembly().Location);
            string pluginDestPath = Path.Combine(opentapPath, "Packages", pluginName);

            var pluginContentSourcePaths = Directory.EnumerateFiles(sourcePath, pyconly ? "*.pyc" : "*.py", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(sourcePath, "*.cs", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(sourcePath, "*.dll", SearchOption.AllDirectories));

            // Create the temporary directories for the plugin tap package to be built in the %OPEN_TAP%\Packages dir
            Directory.CreateDirectory(pluginDestPath);
            var directories = Directory.EnumerateDirectories(sourcePath, "*.*", SearchOption.AllDirectories);
            Parallel.ForEach(directories, dirPath =>
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, pluginDestPath));
            });

            // Copy the plugin content files to the %OPEN_TAP%\Packages dir
            Parallel.ForEach(pluginContentSourcePaths, path =>
            {
                try
                {
                    File.Copy(path, path.Replace(sourcePath, pluginDestPath), true);
                }
                catch (IOException ex)
                {
                    // Suppress temp files in use
                    log.Error($"File copy failed for {path} ({ex.Message})");
                }
            });

            // Create/update the xml file
            string targetXmlFileName = string.IsNullOrWhiteSpace(xml_file_name) ? "package.xml" : (xml_file_name + (Path.GetExtension(xml_file_name) == ".xml" ? "" : ".xml"));
            string targetXmlFilePath = Path.Combine(sourcePath, targetXmlFileName);

            try
            {
                // Collection of .py/.pyc, .dll, .cs files must always be updated to ensure the latest changes are being included in the tap package
                XNamespace aw = @"http://opentap.io/schemas/package";
                XElement files_elem = new XElement(aw + "Files");
                Parallel.ForEach(pluginContentSourcePaths, path =>
                {
                    var fileelem = new XElement(aw + "File");
                    fileelem.SetAttributeValue("Path", path.Replace(sourcePath, $"Packages/{pluginName}"));
                    files_elem.Add(fileelem);
                });

                // Provide default values if the xml file does not exist or the replace flag is true or the existing package element is missing
                if (!File.Exists(targetXmlFilePath)
                    || replace_package_xml
                    || XElement.Load(targetXmlFilePath)?.Name != aw + "Package")
                {
                    // create new xml file
                    var xdoc = new XDocument();
                    XElement package_elem = new XElement(aw + "Package");
                    XElement description = new XElement(aw + "Description");
                    description.Value = "Add a description here";
                    xdoc.Add(package_elem);
                    package_elem.Add(description);
                    package_elem.Add(files_elem);
                    package_elem.SetAttributeValue("InfoLink", "");
                    package_elem.SetAttributeValue("Version", "1.0.0");
                    package_elem.SetAttributeValue("Name", pluginName);
                    package_elem.SetAttributeValue("OS", "Windows,Linux");

                    using (var f = File.Open(targetXmlFilePath, FileMode.Create, FileAccess.ReadWrite))
                    {
                        using (var writer = XmlWriter.Create(f, new XmlWriterSettings() { Indent = true }))
                        {
                            xdoc.WriteTo(writer);
                        }
                    }

                    log.Info("Created file: '{0}'.", targetXmlFilePath);
                }
                else
                {
                    // Just update the files collection if the xml file exists and the replace flag is false and the existing package element exists
                    XElement existing_xml_content = XElement.Load(targetXmlFilePath);
                    XElement existing_files = existing_xml_content.Element(aw + "Files");

                    if (existing_files == null)
                        existing_xml_content.Add(files_elem);
                    else
                        existing_files.ReplaceWith(files_elem);

                    existing_xml_content.Save(targetXmlFilePath);
                    log.Info("Updated file: '{0}'.", targetXmlFilePath);
                }

                createPackage(targetXmlFilePath, pluginName + ".TapPackage");
            }
            catch (Exception e)
            {
                log.Error(e.Message);
            }
            finally
            {
                // delete the temporary plugin folder created to build the tap package
                Directory.Delete(pluginDestPath, true);
            }
        }

        private static void createPackage(string inputXmlPath, string outputPath)
        {
            //generating package by using command line
            string destPluginPackageFile = Path.Combine(Path.GetDirectoryName(inputXmlPath), outputPath);
            string packageManagerFile = Path.Combine(Path.GetDirectoryName(PluginManager.GetOpenTapAssembly().Location), PyThread.IsWin32 ? "tap.exe" : "tap");
            string argument = "package create " + "\"" + inputXmlPath + "\""
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
                log.Info("Saved to '{0}'.", destPluginPackageFile);
            }
            else
            {
                log.Error(string.Format("Failed generating package with exit code {0}:{1} ", exitcode, error));
            }
        }
    }
}
