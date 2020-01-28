#region Imports
import clr
import os
import sys
import time
import abc
import threading
from threading import Thread
import thread
import time

tapPath = os.environ["TAP_PATH"]
sys.path.append(tapPath)
print clr.__file__
clr.AddReference("OpenTap")
clr.AddReference("OpenTap.Plugins.BasicSteps")
clr.AddReference("System")
clr.AddReference("Keysight.OpenTap.Plugins.Python.PyHelper")

from abc import *
from System.Diagnostics import *
from System.Collections.Generic import List
from OpenTap import *
from OpenTap.Plugins.BasicSteps import *
from Keysight.OpenTap.Plugins.Python import *
#endregion

##################################################################
#ResultListener overrides
##################################################################
def PyOpen():
    pass

def PyOnResultPublished(stepRunID, result):
    pass

def PyOnTestPlanRunStart(planRun):
    pass

def PyOnTestStepRunStart(stepRun):
    global guiFrame
    print stepRun.TestStepName + " started"

def PyOnTestStepRunCompleted(stepRun):
    pass

def PyOnTestPlanRunCompleted(planRun, logStream):
    print "Test plan run completed with Verdict: " + verdictDictionary[planRun.Verdict]

def PyClose():
    pass
##################################################################

#Required to translate verdict from enum to string
verdictDictionary = { Verdict.NotSet : "NotSet",
                      Verdict.Pass : "Pass",
                      Verdict.Inconclusive : "Inconclusive",
                      Verdict.Fail : "Fail",
                      Verdict.Aborted : "Aborted",
                      Verdict.Error : "Error",}

#TAP Logging event handler
def EventsLogged(events):
    for event in events:
        #Filter out verbose messages
        #if event.EventType != TraceEventType.Verbose:
        print event.Message
    return

class InitLogListener():
    logListener = None

    def __init__(self):
        self.logListener = PyHelper.LogListener()
        self.logListener.PyEventsLogged += EventsLogged

testPlanFilePath = None
logListener = InitLogListener()

def InitializeTap():
    PluginManager.Search()
    print "Plugins found:"
    for plugin in PluginManager.GetAllPlugins():
        print(plugin.Name)
    SessionLogs.Load("TapPythonConsole_log.txt")
    return

def InitializeResultListeners():
    myResultListener = PyHelper.ResultListener()
    myResultListener.WOpen += PyOpen
    myResultListener.WOnResultPublished += PyOnResultPublished
    myResultListener.WOnTestStepRunStart += PyOnTestStepRunStart
    myResultListener.WOnTestStepRunCompleted += PyOnTestStepRunCompleted
    myResultListener.WOnTestPlanRunStart += PyOnTestPlanRunStart
    myResultListener.WOnTestPlanRunCompleted += PyOnTestPlanRunCompleted
    myResultListener.WClose += PyClose
    resultListenersList = List[PyHelper.ResultListener]()
    resultListenersList.Add(myResultListener)
    return resultListenersList

def TestPlanExecutionThread(testPlan, list):
    testPlan.Execute(list)
    return

def main(argv):
    InitializeTap()

    if len(argv) > 1:
        testPlanFilePath = argv[1]
    else:
        testPlanFilePath = "Delay-Verdict.TapPlan"

    #Load and run test plan
    testPlan = TestPlan.Load(testPlanFilePath);

    testPlanName = str(os.path.basename(testPlanFilePath))
    print "Test Plan: " + testPlanName

    resultListenersList = InitializeResultListeners()

    try:
        testPlanExecutionThread = threading.Thread(target=TestPlanExecutionThread, args=(testPlan, resultListenersList))
        testPlanExecutionThread.start()
        print "Running Test Plan"
        testPlanExecutionThread.join()

    except Exception as inst:
        print "Caught Exception: "
        print type(inst)     # the exception instance
        print inst.args      # arguments stored in .args
        print inst           # __str__ allows args to be printed directly

    sys.exit(0)
    return

if __name__ == "__main__":
    main(sys.argv)
