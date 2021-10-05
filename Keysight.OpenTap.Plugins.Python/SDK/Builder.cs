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
                try
                {
                    buildTapPackage(actualModulePath, pyconly, replace_package_xml, dump_package_xml);
                }
                catch (Exception ex)
                {
                    log.Error("Cannot build the TapPackage file - {0}", ex.Message);
                }
            }
            log.Info("{0} Completed.", modulename);
            return exitcode;
        }

        // buildTapPlugin needs to be encapsulated, because it depends on Keysight.Tap.Package
        // if the exe is missing, any method using it will throw an exception.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void buildTapPackage(string source_path, bool pyconly, bool replace_package_xml, string dump_package_xml)
        {
            DirectoryInfo source_path_info = new DirectoryInfo(source_path);
            string open_tap_path = Path.GetDirectoryName(PluginManager.GetOpenTapAssembly().Location);
            string plugin_name = source_path_info.Name;
            bool is_open_tap_path = string.Compare(Directory.GetCurrentDirectory(), open_tap_path) == 0;
            string target_plugin_path = Path.Combine(open_tap_path, "Packages", "Python", plugin_name);
            bool is_ext_module = string.Compare(source_path, target_plugin_path) != 0;
            IEnumerable<string> file_source_paths = Directory.EnumerateFiles(source_path, pyconly ? "*.pyc" : "*.py", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(source_path, "*.dll", SearchOption.AllDirectories));

            // Create/update the xml file
            string target_xml_file_name = string.IsNullOrWhiteSpace(dump_package_xml) ? "package.xml" : (Path.GetFileNameWithoutExtension(dump_package_xml) + ".xml");
            string target_xml_file_path = Path.Combine(source_path, target_xml_file_name);

            try
            {
                // Collection of .py/.pyc, .dll, .cs files must always be updated to ensure the latest changes are being included in the tap package
                XNamespace aw = @"http://opentap.io/schemas/package";
                XElement new_files_elem = new XElement(aw + "Files");
                
                // create a new files collection
                foreach (string file_source_path in file_source_paths)
                {
                    var new_file_elem = new XElement(aw + "File");

                    // Path attribute has to be relative to OpenTAP path for package distribution
                    new_file_elem.SetAttributeValue("Path", file_source_path.Replace(source_path, Path.Combine("Packages", "Python", plugin_name)));

                    // If the current working dir is not OpenTAP dir, we set the SourcePath attribute to the absoulte path for Package Manager
                    // The Package Manager will throw 'File not found' error if the SourcePath is not set
                    if (!is_open_tap_path || is_ext_module)
                        new_file_elem.SetAttributeValue("SourcePath", file_source_path);

                    new_files_elem.Add(new_file_elem);
                }

                // Provide default values if the xml file does not exist or the replace flag is true or the existing package element is missing
                if (!File.Exists(target_xml_file_path)
                    || replace_package_xml
                    || XElement.Load(target_xml_file_path)?.Name != aw + "Package")
                {
                    // create new xml file
                    var xdoc = new XDocument();
                    XElement package_elem = new XElement(aw + "Package");
                    XElement description = new XElement(aw + "Description");
                    description.Value = "Add a description here";
                    xdoc.Add(package_elem);
                    package_elem.Add(description);
                    package_elem.Add(new_files_elem);
                    package_elem.SetAttributeValue("InfoLink", "");
                    package_elem.SetAttributeValue("Version", "1.0.0");
                    package_elem.SetAttributeValue("Name", plugin_name);
                    package_elem.SetAttributeValue("OS", "Windows,Linux");

                    using (var f = File.Open(target_xml_file_path, FileMode.Create, FileAccess.ReadWrite))
                    {
                        using (var writer = XmlWriter.Create(f, new XmlWriterSettings() { Indent = true }))
                        {
                            xdoc.WriteTo(writer);
                        }
                    }

                    log.Info("Created file: '{0}'.", target_xml_file_path);
                }
                else
                {
                    // Just update the files element if the xml file exists and the replace flag is false and the existing package element exists
                    XElement existing_xml_doc = XElement.Load(target_xml_file_path);
                    XElement existing_files_elem = existing_xml_doc.Element(aw + "Files");

                    if (existing_files_elem == null)
                    {
                        // add the new files elem if the existing files elem is not found
                        existing_xml_doc.Add(new_files_elem);
                    }
                    else
                    {
                        // if the exisitng files elem is found, we update its descendants
                        var latest_files_elem = new XElement(aw + "Files");
                        foreach (var existing_file_elem in existing_files_elem.Descendants(aw + "File"))
                        {
                            var sourcePathAttbValue = existing_file_elem.Attribute("SourcePath")?.Value;
                            var pathAttbValue = existing_file_elem.Attribute("Path") != null ? Path.Combine(open_tap_path, existing_file_elem.Attribute("Path").Value) : null;

                            // keep the existing file element if it exists on the disk
                            if (File.Exists(sourcePathAttbValue) || File.Exists(pathAttbValue))
                            {
                                latest_files_elem.Add(existing_file_elem);
                            }
                        }

                        // add the new file element
                        foreach (var new_file_elem in new_files_elem.Descendants(aw + "File"))
                        {
                            // to verify if the new file elem exists in the latest files elem
                            var existing_file_elem = latest_files_elem.Descendants(aw + "File").Where(x => x.Attribute("Path")?.Value == new_file_elem.Attribute("Path")?.Value).FirstOrDefault();

                            if (existing_file_elem == null)
                            {
                                // add new file elem
                                latest_files_elem.Add(new_file_elem);
                            }
                            else if (!string.IsNullOrWhiteSpace(new_file_elem.Attribute("SourcePath")?.Value))
                            {
                                // update the source path if the existing file elem is found
                                existing_file_elem.SetAttributeValue("SourcePath", new_file_elem.Attribute("SourcePath")?.Value);
                            }
                        }

                        // replace with the latest files elem
                        existing_files_elem.ReplaceWith(latest_files_elem);
                    }
                    existing_xml_doc.Save(target_xml_file_path);
                    log.Info("Updated file: '{0}'.", target_xml_file_path);
                }
                createPackage(target_xml_file_path, plugin_name + ".TapPackage");
            }
            catch (Exception e)
            {
                log.Error(e.Message);
            }
            finally
            {
                // delete the temporary plugin folder created by the Package Manager if the module is outside of the TAP_PATH.
                var temp_plugin_dir = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "Python", plugin_name);
                if((is_ext_module || !is_open_tap_path) && Directory.Exists(temp_plugin_dir))
                {
                    Directory.Delete(temp_plugin_dir, true);
                }
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
                log.Info("Created package '{0}'.", destPluginPackageFile);
            }
            else
            {
                log.Error(string.Format("Failed generating package with exit code {0}:{1} ", exitcode, error));
            }
        }
    }
}
