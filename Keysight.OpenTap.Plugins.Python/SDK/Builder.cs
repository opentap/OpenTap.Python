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
        public static ExitCodes doBuild(string moduleName, bool includePyc, bool buildTapPlugin, bool replacePackageXml, string dumpPackageXml)
        {
            if (WrapperBuilder.LoadPython() == false)
                return ExitCodes.UnableToLoadPython;
            return doBuildint(moduleName, includePyc, buildTapPlugin, replacePackageXml, dumpPackageXml);
        }


        public static ExitCodes doBuildint(string moduleName, bool includePyc, bool buildTapPlugin, bool replacePackageXml, string dumpPackageXml)
        {
            log.Info("Start building {0}.", moduleName);
            new PythonSettings();
            var pluginSearchPaths = PythonSettings.Current.GetSearchPaths().Distinct();
            var foundPluginPaths = new List<string>();
            var exitcode = ExitCodes.Success;
            string defaultModulePath = Path.Combine(Path.GetDirectoryName(typeof(Builder).Assembly.Location), moduleName);
            string actualModulePath = "";

            foreach (var chr in moduleName.Distinct())
            {
                if (pythonModuleNameValidator(chr) == false)
                {
                    log.Error("The module name cannot contain '{0}'. Char code: {1}", chr, (int)chr);
                    exitcode = ExitCodes.InvalidModuleName;
                }
            }

            if (char.IsDigit(moduleName[0]))
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
                else if (Directory.Exists(Path.Combine(path, moduleName)))
                    foundPluginPaths.Add(Path.Combine(path, moduleName));
            }

            foundPluginPaths = foundPluginPaths.Distinct().ToList();

            if (foundPluginPaths.Count == 0 && !Directory.Exists(defaultModulePath))
            {
                log.Error($"The plugin module is not found: '{moduleName}'.");
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
                var dllFileName = Path.Combine(actualModulePath, "Python." + moduleName + ".dll");
                new WrapperBuilder().Build(new List<string> { moduleName }, dllFileName, false, moduleName);
                var dllLegacyFileName = Path.Combine(actualModulePath, moduleName + ".dll");
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
            catch (UnauthorizedAccessException uae)
            {
                log.Error("Please close any active tap processes such as Editor, Editor X and others, and build the python plugin again.\n" + uae.Message);
                return ExitCodes.UnableToBuildWrapper;
            }
            catch (Exception ex)
            {
                log.Error("Caught exception while creating plugin.");
                log.Debug(ex);
                Log.Flush();
                return ExitCodes.UnableToBuildWrapper;
            }

            bool pyconly = includePyc;

            if (buildTapPlugin)
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
                buildTapPackage(actualModulePath, pyconly, replacePackageXml, dumpPackageXml);
            }
            log.Info("{0} Completed.", moduleName);
            return exitcode;
        }

        // buildTapPlugin needs to be encapsulated, because it depends on Keysight.Tap.Package
        // if the exe is missing, any method using it will throw an exception.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void buildTapPackage(string sourcePath, bool pycOnly, bool replacePackageXml, string dumpPackageXml)
        {
            DirectoryInfo sourcePathInfo = new DirectoryInfo(sourcePath);
            string openTapPath = Path.GetDirectoryName(PluginManager.GetOpenTapAssembly().Location);
            string pluginName = sourcePathInfo.Name;
            bool isOpenTapPath = string.Compare(Directory.GetCurrentDirectory(), openTapPath) == 0;
            string targetPluginPath = Path.Combine(openTapPath, "Packages", "Python", pluginName);
            bool isExtModule = string.Compare(sourcePath, targetPluginPath) != 0;
            IEnumerable<string> fileSourcePaths = Directory.EnumerateFiles(sourcePath, pycOnly ? "*.pyc" : "*.py", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(sourcePath, "*.dll", SearchOption.AllDirectories));

            // Create/update the xml file
            string targetXmlFileName = string.IsNullOrWhiteSpace(dumpPackageXml) ? "package.xml" : (Path.GetFileNameWithoutExtension(dumpPackageXml) + ".xml");
            string targetXmlFilePath = Path.Combine(sourcePath, targetXmlFileName);

            try
            {
                // Collection of .py/.pyc, .dll, .cs files must always be updated to ensure the latest changes are being included in the tap package
                XNamespace aw = @"http://opentap.io/schemas/package";
                XElement newFilesElem = new XElement(aw + "Files");
                
                // create a new files collection
                foreach (string fileSourcePath in fileSourcePaths)
                {
                    var newFileElem = new XElement(aw + "File");

                    // Path attribute has to be relative to OpenTAP path for package distribution
                    newFileElem.SetAttributeValue("Path", fileSourcePath.Replace(sourcePath, Path.Combine("Packages", "Python", pluginName)));

                    // If the current working dir is not OpenTAP dir, we set the SourcePath attribute to the absoulte path for Package Manager
                    // The Package Manager will throw 'File not found' error if the SourcePath is not set
                    if (!isOpenTapPath || isExtModule)
                        newFileElem.SetAttributeValue("SourcePath", fileSourcePath);

                    newFilesElem.Add(newFileElem);
                }

                // Provide default values if the xml file does not exist or the replace flag is true or the existing package element is missing
                if (!File.Exists(targetXmlFilePath)
                    || replacePackageXml
                    || XElement.Load(targetXmlFilePath)?.Name != aw + "Package")
                {
                    // create new xml file
                    var xdoc = new XDocument();
                    XElement packageElem = new XElement(aw + "Package");
                    XElement description = new XElement(aw + "Description");
                    description.Value = "Add a description here";
                    xdoc.Add(packageElem);
                    packageElem.Add(description);
                    packageElem.Add(newFilesElem);
                    packageElem.SetAttributeValue("InfoLink", "");
                    packageElem.SetAttributeValue("Version", "1.0.0");
                    packageElem.SetAttributeValue("Name", pluginName);
                    packageElem.SetAttributeValue("OS", "Windows,Linux");

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
                    // Just update the files element if the xml file exists and the replace flag is false and the existing package element exists
                    XElement existingXmlDoc = XElement.Load(targetXmlFilePath);
                    XElement existingFilesElem = existingXmlDoc.Element(aw + "Files");

                    if (existingFilesElem == null)
                    {
                        // add the new files elem if the existing files elem is not found
                        existingXmlDoc.Add(newFilesElem);
                    }
                    else
                    {
                        // if the exisitng files elem is found, we update its descendants
                        var latestFilesElem = new XElement(aw + "Files");
                        foreach (var existingFileElem in existingFilesElem.Descendants(aw + "File"))
                        {
                            var sourcePathAttbValue = existingFileElem.Attribute("SourcePath")?.Value;
                            var pathAttbValue = existingFileElem.Attribute("Path") != null ? Path.Combine(openTapPath, existingFileElem.Attribute("Path").Value) : null;

                            // keep the existing file element if it exists on the disk
                            if (File.Exists(sourcePathAttbValue) || File.Exists(pathAttbValue))
                            {
                                latestFilesElem.Add(existingFileElem);
                            }
                        }

                        // add the new file element
                        foreach (var newFileElem in newFilesElem.Descendants(aw + "File"))
                        {
                            // to verify if the new file elem exists in the latest files elem
                            var existingFileElem = latestFilesElem.Descendants(aw + "File").Where(x => x.Attribute("Path")?.Value == newFileElem.Attribute("Path")?.Value).FirstOrDefault();

                            if (existingFileElem == null)
                            {
                                // add new file elem
                                latestFilesElem.Add(newFileElem);
                            }
                            else if (!string.IsNullOrWhiteSpace(newFileElem.Attribute("SourcePath")?.Value))
                            {
                                // update the source path if the existing file elem is found
                                existingFileElem.SetAttributeValue("SourcePath", newFileElem.Attribute("SourcePath")?.Value);
                            }
                        }

                        // replace with the latest files elem
                        existingFilesElem.ReplaceWith(latestFilesElem);
                    }
                    existingXmlDoc.Save(targetXmlFilePath);
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
                // delete the temporary plugin folder created by the Package Manager if the module is outside of the TAP_PATH.
                var tempPluginDir = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "Python", pluginName);
                if((isExtModule || !isOpenTapPath) && Directory.Exists(tempPluginDir))
                {
                    Directory.Delete(tempPluginDir, true);
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
