"""
This relatively simple test executive script loads the first command line argument as a test plan.
it includes tap_example2.py to demonstrate that breakpoints can be set inside plugin code.
"""
import clr
import System.IO
import sys
planpath = sys.argv[1]
sys.path.append(System.IO.Directory.GetCurrentDirectory())
clr.AddReference("OpenTap")
clr.AddReference("OpenTap.Cli")
import OpenTap.Plugins.BasicSteps
import OpenTap
import OpenTap.Cli
OpenTap.PluginManager.ApplicationBaseDirectory = System.IO.Directory.GetCurrentDirectory()
OpenTap.Log.AddListener(OpenTap.Cli.CliTraceListener(True, False, True))
OpenTap.PluginManager.DirectoriesToSearch.Clear();
OpenTap.PluginManager.DirectoriesToSearch.Add(".");
OpenTap.ComponentSettings.SettingsDirectoryRoot = System.IO.Path.Combine([System.IO.Directory.GetCurrentDirectory(), "Settings"])
OpenTap.PluginManager.SearchAsync()

import tap
import PluginExample

plan = OpenTap.TestPlan.Load(planpath)
run = plan.Execute()