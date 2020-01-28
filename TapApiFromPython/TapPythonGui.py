#region Imports
#importing wx files
import wx
#import the newly created GUI file
import wxGuiClass

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
clr.AddReference("Keysight.OpenTap.Plugins.BasicSteps")
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
  guiFrame.listBox_LogPanel.AppendAndEnsureVisible(stepRun.TestStepName + "Started")

def PyOnTestStepRunCompleted(stepRun):
  pass

def PyOnTestPlanRunCompleted(planRun, logStream):
    print verdictDictionary[planRun.Verdict]
    print "Test plan completed"

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

#Global Gui object
guiFrame = None

def AddMessagesToLogPanel(event):
    global guiFrame
    guiFrame.listBox_LogPanel.AppendAndEnsureVisible(event.Message)
    lines = guiFrame.listBox_LogPanel.GetCount() - 1
    if lines > 0:
        guiFrame.listBox_LogPanel.SetSelection(lines, True)
        guiFrame.listBox_LogPanel.SetSelection(lines, False)

#TAP Logging event handler
def EventsLogged(events):
    global guiFrame
    for event in events:
        #Filter out verbose messages
        #if event.EventType != TraceEventType.Verbose:
        print event.Message
        wx.CallAfter(AddMessagesToLogPanel, event)

class InitLogListener():
    logListener = None

    def __init__(self, tapGuiFrame):
        self.logListener = PyHelper.LogListener()
        self.logListener.PyEventsLogged += EventsLogged

#inherit from the MainFrame created in wxFormBuilder
class TapGuiFrame(wxGuiClass.MainFrame):
    logListener = None
    #constructor
    def __init__(self,parent):
        #initialize parent class
        wxGuiClass.MainFrame.__init__(self,parent)
        return

    def InitializeTap(self):
        PluginManager.Search()
        #for plugin in PluginManager.GetAllPlugins():
            #print(plugin.Name)
        SessionLogs.Load("TapPythonGui_log.txt")
        return

    def InitializeResultListeners(self):
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

    def TestPlanExecutionThread(self, testPlan, list):
        testPlan.Execute(list)
        return

    def RunTestPlan_Clicked(self, event):
        try:
            #init
            #Set TestPlan FilePath
            testPlanFilePath = self.Ctrl_BrowseForTestPlanFile.GetPath()

            if testPlanFilePath == "":
                return
            #    self.m_statusBar1.SetStatusText("Error")
            #    self.listBox_LogPanel.AppendAndEnsureVisible("Error: File Missing or Invalid Filename")
            #    raise BaseException("File Missing or Invalid Filename")

            #Clear the Logpanel before next run
            self.listBox_LogPanel.Clear()
            self.m_statusBar1.SetStatusText("Initializing...")
            self.InitializeTap()

            #Load and run test plan
            testPlan = TestPlan.Load(testPlanFilePath);

            testPlanName = str(os.path.basename(testPlanFilePath))
            #self.listBox_LogPanel.AppendAndEnsureVisible("Loaded File: " + testPlanName)
            self.m_statusBar1.SetStatusText("Test Plan: " + testPlanName)

            resultListenersList = self.InitializeResultListeners()

            try:
                testPlanExecutionThread = threading.Thread(target=self.TestPlanExecutionThread, args=(testPlan, resultListenersList))
                testPlanExecutionThread.start()

            except Exception as inst:
                print "Caught Exception: "
                print type(inst)     # the exception instance
                print inst.args      # arguments stored in .args
                print inst           # __str__ allows args to be printed directly

            self.listBox_LogPanel.AppendAndEnsureVisible("Running Test Plan")

            #print testPlanRun.Verdict

            print "Test Plan Run Completed"

            self.m_statusBar1.SetStatusText("Ready")

        except Exception as inst:
            print "Caught Exception: "
            print type(inst)     # the exception instance
            print inst.args      # arguments stored in .args
            print inst           # __str__ allows args to be printed directly

        return

def main(argv):
    #Create a wx app, False stands for not redirecting stdin/stdout
    app = wx.App(False)
    
    global guiFrame
    
    #create an object
    frame = TapGuiFrame(None)
    guiFrame = frame

    initLogListener = InitLogListener(frame)

    #show the frame
    frame.Show(True)
    #start the application
    app.MainLoop()
    sys.exit(0)
    return

if __name__ == "__main__":
    main(sys.argv)
