#region Imports
import clr
import os
import sys
import time
tapPath = os.environ["TAP_PATH"]
sys.path.append(tapPath)

clr.AddReference("System")
clr.AddReference("OpenTap")
clr.AddReference("OpenTap.Plugins.BasicSteps")

from System import TimeSpan
from System.IO import FileStream
from System.IO import FileMode
from OpenTap import *
from OpenTap.Plugins.BasicSteps import *
#endregion

def main(argv):
    #Required to translate verdict from enum to string
    verdictDictionary = { Verdict.NotSet : "NotSet",
                          Verdict.Pass : "Pass",
                          Verdict.Inconclusive : "Inconclusive",
                          Verdict.Fail : "Fail",
                          Verdict.Aborted : "Aborted",
                          Verdict.Error : "Error",}

    if len(argv) > 1:
        testPlanFilePath = argv[1]
    else:
        testPlanFilePath = "Delay-Verdict.TapPlan"

    print "Plugins Found:"
    for plugin in PluginManager.GetAllPlugins():
        print plugin.Name

    print "\nTest Plan Run Started"

    # Required to find plugins
    PluginManager.Search()

    SessionLogs.Load("TapPythonConsoleBasic_log.txt")

    print "SessionLog: " + SessionLogs.GetSessionLogFilePath()

    testPlan = TestPlan.Load(testPlanFilePath);
    print "TestPlan Name: " + testPlan.Name

    testPlanRun = testPlan.Execute()

    print "Test Plan Run Completed"
    print "Verdict: " + verdictDictionary[testPlanRun.Verdict]
    sys.exit(0)
    return

if __name__ == "__main__":
    main(sys.argv)
