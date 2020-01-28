#region Imports
import wx
#import the newly created GUI file
import wxGuiClass

import clr
import os
import sys
import time
import abc
import time
import threading
from threading import Thread

tapPath = os.environ["TAP_PATH"]
sys.path.append(tapPath)
print clr.__file__
clr.AddReference("System")
clr.AddReference("OpenTap")
clr.AddReference("Keysight.OpenTap.Plugins.BasicSteps")

from abc import *
from System.Diagnostics import *
from OpenTap import *
from OpenTap.Plugins.BasicSteps import *
#endregion

#inherit from the MainFrame created in wxFormBuilder
class TapGuiFrame(wxGuiClass.MainFrame):
    logListener = None
    #Required to translate verdict from enum to string
    verdictDictionary = { Verdict.NotSet : "NotSet",
                          Verdict.Pass : "Pass",
                          Verdict.Inconclusive : "Inconclusive",
                          Verdict.Fail : "Fail",
                          Verdict.Aborted : "Aborted",
                          Verdict.Error : "Error",}

    #constructor
    def __init__(self,parent):
        #initialize parent class
        wxGuiClass.MainFrame.__init__(self,parent)
        return

    def InitializeTap(self):
        PluginManager.Search()
        #for plugin in PluginManager.GetAllPlugins():
            #print(plugin.Name)
        SessionLogs.Load("TapPython_log.txt")
        return

    def TestPlanExecutionThread(self, testPlan):
        testPlan.Execute()
        return

    def RunTestPlan_Clicked(self, event):
        #init
        #Set TestPlan FilePath
        testPlanFilePath = self.Ctrl_BrowseForTestPlanFile.GetPath()

        if testPlanFilePath == "":
            return
        #Clear the Logpanel before next run
        self.listBox_LogPanel.Clear()
        self.m_statusBar1.SetStatusText("Initializing...")
        self.listBox_LogPanel.AppendAndEnsureVisible("Initializing...")

        self.InitializeTap()
  
        #Load and run test plan
        testPlan = TestPlan.Load(testPlanFilePath);

        testPlanName = str(os.path.basename(testPlanFilePath))
        self.listBox_LogPanel.AppendAndEnsureVisible("Loaded TestPlan File: " + testPlanName)
        self.m_statusBar1.SetStatusText("Test Plan: " + testPlanName)
        self.listBox_LogPanel.AppendAndEnsureVisible("Executing Test Plan...")
        self.Update()
            
        try:
            #wx.CallAfter(self.TestPlanExecutionThread, testPlan)
            #testPlanExecutionThread = threading.Thread(target=self.TestPlanExecutionThread, args=(testPlan,))
            testPlanRun = testPlan.Execute()
            #testPlanExecutionThread.start()

        except Exception as inst:
            print "Caught Exception: "
            print type(inst)     # the exception instance
            print inst.args      # arguments stored in .args
            print inst           # __str__ allows args to be printed directly

        #testPlanExecutionThread.join()

        self.listBox_LogPanel.AppendAndEnsureVisible("Verdict: " + self.verdictDictionary[testPlanRun.Verdict])

        self.listBox_LogPanel.AppendAndEnsureVisible("Test Plan Run Completed")
        self.Update()
        self.m_statusBar1.SetStatusText("Ready")

def main(argv):
    #Create a wx app, False stands for not redirecting stdin/stdout
    app = wx.App(False)

    #create an object
    frame = TapGuiFrame(None)
    guiFrame = frame

    #show the frame
    frame.Show(True)
    #start the application
    app.MainLoop()
    sys.exit(0)
    return

if __name__ == "__main__":
    main(sys.argv)
