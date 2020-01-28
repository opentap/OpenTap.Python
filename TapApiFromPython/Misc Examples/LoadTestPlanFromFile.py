import clr
import os
import sys
import time
tapPath = os.environ["TAP_PATH"]
sys.path.append(tapPath)

clr.AddReference("OpenTap")
clr.AddReference("Keysight.OpenTap.Plugins.BasicSteps")
clr.AddReference("System")
from System.IO import FileStream
from System.IO import FileMode
from System import TimeSpan
from OpenTap import *
from Keysight.OpenTap.Plugins.BasicSteps import *

for plugin in PluginManager.GetAllPlugins():
    print(plugin.Name)

# If you have plugins in directories different from the location of Engine.dll, then add those directories here.
# PluginManager.DirectoriesToSearch.Add(@"C:\SomeOtherDirectory");

# Required to find plugins
print "Test Plan Started"
PluginManager.Search()

SessionLogs.Load("console_log.txt")

myTestPlan = TestPlan.Load(tapPath + "\\" + "MyTestPlan.TapPlan");

myTestPlan.Execute()

print "Test Plan Ended"

