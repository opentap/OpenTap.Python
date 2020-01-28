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


tapPath = os.environ["TAP_PATH"]
sys.path.append(tapPath)
print clr.__file__
clr.AddReference("OpenTap")
clr.AddReference("OpenTap.Plugins.BasicSteps")
clr.AddReference("System")
clr.AddReference("Keysight.OpenTap.Plugins.Python.PyHelper")
from OpenTap import *
from Keysight.OpenTap.Plugins.BasicSteps import *
from abc import *
from System.Diagnostics import *
import time

from Keysight.OpenTap.Plugins.Python import *
from System.Collections.Generic import List

class ChildThread(Thread):
    def __init__(self, myframe):
        ##Init Worker Thread Class.
        Thread.__init__(self)
        self.myframe = myframe

    def run(self):
        wx.CallAfter(self.myframe.AfterRun, 'Ok button pressed')

def PyOpen():
  pass
#  print "Open"
def PyOnResultPublished(stepRunID, result):
  pass
#  print "Published"
def PyOnTestPlanRunStart(planRun):
  print planRun.TestPlanName, "started"
  pass
#  print "TestPlanRunStart"
def PyOnTestPlanRunCompleted(planRun, logStream):
  if planRun.Verdict == Verdict.NotSet:
    print "NotSet"
  elif planRun.Verdict == Verdict.Pass:
    print "Pass"
  elif planRun.Verdict == Verdict.Inconclusive:
    print "Inconclusive"
  elif planRun.Verdict == Verdict.Fail:
    print "Fail"
  elif planRun.Verdict == Verdict.Aborted:
    print "Aborted"
  elif planRun.Verdict == Verdict.Error:
    print "Error"
  print "Test plan completed"
#  print "TestPlanRunCompleted"
def PyOnTestStepRunCompleted(stepRun):
  pass
#  print "TestStepRunCompleted"
def PyOnTestStepRunStart(stepRun):
  print stepRun.TestStepName, "started!!!!!!!!!!!!!!!!!!!!!!!"
  wx.CallAfter(guiFrame.listBox_LogPanel.AppendAndEnsureVisible, stepRun.TestStepName + "started!!!!")
#  print "TestStepRunStart"
def PyClose():
  pass

guiFrame = None

def ListBoxTest(event):
    global guiFrame
    guiFrame.listBox_LogPanel.AppendAndEnsureVisible(event.Message)
    lines = guiFrame.listBox_LogPanel.GetCount() - 1
    print lines
    if lines > 0:
        guiFrame.listBox_LogPanel.SetSelection(lines, True)
        guiFrame.listBox_LogPanel.SetSelection(lines, False)
        print "Moved selection!!!!!!!!!!!!!!!!!!!!!!"

def EventsLogged(events):
    global guiFrame
    print "Before selection!!!!!!!!!!!!!!!!!!!!!!"
    #print "EventsLogged called"
    #print id(guiFrame)
    #print "guiFrame!!!: ", id(guiFrame.listBox_LogPanel)
    for event in events:
        #if event.EventType != TraceEventType.Verbose:
        print event.Message
        #ObjectName.SetLabel(Message)
        #wx.CallAfter(ObjectName.SetLabel, Message)
        #guiFrame.listBox_LogPanel.AppendAndEnsureVisible("Hello")
        wx.CallAfter(ListBoxTest, event)

class PyClass():
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
        #self.logListener = PyHelper.LogListener()
        #self.logListener.PyEventsLogged += self.EventsLogged
        #self.listBox_LogPanel.Bind(wx.EVT_SCROLLWIN, self.OnScrollEvt)

    #def OnScrollEvt(self, event):
    #    pass

    def InitializeTap(self):
        PluginManager.Search()
        #for plugin in PluginManager.GetAllPlugins():
            #print(plugin.Name)
        SessionLogs.Load("console_log.txt")

    #def onRun2(self, event):
    #    self.child2 = threading.Thread(None, self.__run)
    #    self.child2.daemon = True
    #    self.child2.start()

    #def AfterRun(self, msg):
    #    pass

    #def __run(self):
    #    wx.CallAfter(self.AfterRun, "Cancel button pressed")



    def InitializeResultListeners(self):
        myResultListener = PyHelper.ResultListener()
        myResultListener.WOpen += PyOpen
        myResultListener.WOnResultPublished += PyOnResultPublished
        myResultListener.WOnTestStepRunStart += PyOnTestStepRunStart
        myResultListener.WOnTestStepRunCompleted += PyOnTestStepRunCompleted
        myResultListener.WOnTestPlanRunStart += PyOnTestPlanRunStart
        myResultListener.WOnTestPlanRunCompleted += PyOnTestPlanRunCompleted
        myResultListener.WClose += PyClose
        myList = List[PyHelper.ResultListener]()
        myList.Add(myResultListener)

        return myList

    def ScrollMe(self, lines):
        self.listBox_LogPanel.SetScrollPos(wx.VERTICAL, lines - 1, True)

    def AfterRun(self, testPlan, list):
        #print "TestPlan is!!!: " + id(testPlan) + " " + id(list) + list.Count()
        testPlan.Execute(list)

    def MyThread(self, testPlan, list):
        print "BEGINN!!!!!!" + str(id(testPlan))
        #print "TestPlan is!!!: " + id(testPlan) + " " + id(list) + list.Count()
        #wx.CallAfter(self.AfterRun, testPlan, list)
        #self.AfterRun(testPlan, list)
        testPlan.Execute(list)

    def RunTestPlan_Clicked(self, event):
        try:
            #Code to load and run test plan
            #print "Self: ", id(self.listBox_LogPanel)
            #init
            self.m_statusBar1.SetStatusText("Initializing...")

            self.InitializeTap()

            self.m_statusBar1.SetStatusText("Ready")

            #Set TestPlan FilePath
            #testPlanFilePath = self.Ctrl_BrowseForTestPlanFile.GetPath()
            #if testPlanFilePath == "":
            #    testPlanFilePath = 
            testPlanFilePath = r"C:\git\tap-plugins\cpython\TapApi\Delay-Verdict.TapPlan"
            self.Ctrl_BrowseForTestPlanFile.SetPath(testPlanFilePath)
            self.listBox_LogPanel.Clear()
            myTestPlan = TestPlan.Load(testPlanFilePath);

            testPlanName = str(os.path.basename(testPlanFilePath))
            #self.listBox_LogPanel.AppendAndEnsureVisible("Loaded File: " + testPlanName)
            self.m_statusBar1.SetStatusText("Test Plan: " + testPlanName)

            myList = self.InitializeResultListeners()

            #testPlanRun = myTestPlan.Execute(myList)
            #wx.CallAfter(self.AfterRun, myTestPlan, myList)

            try:
                #thread.start_new_thread(self.MyThread, args=(myTestPlan, myList))
                myThread = threading.Thread(target=self.MyThread, args=(myTestPlan, myList))
                myThread.start()
                #wx.CallAfter(myTestPlan.Execute, myList)
                #myTestPlan.Execute(myList)
            except Exception as inst:
                print type(inst)     # the exception instance
                print inst.args      # arguments stored in .args
                print inst           # __str__ allows args to be printed directly
                print "Exception from myThread"

            self.listBox_LogPanel.AppendAndEnsureVisible("Running Test Plan")

            #print testPlanRun.Verdict

            print "Test Plan Ended"
            #print 'RunTestPlanClicked'

            self.listBox_LogPanel.AppendAndEnsureVisible("Ready")
            self.m_statusBar1.SetStatusText("Ready")

        except Exception as inst:
            print type(inst)     # the exception instance
            print inst.args      # arguments stored in .args
            print inst           # __str__ allows args to be printed directly
            print 'Caught Exception'

    #Clear the Logpanel before next run
        #self.text.SetValue(str(''))
#mandatory in wx, create an app, False stands for not redirecting stdin/stdout
#refer manual for details
app = wx.App(False)

#create an object
frame = TapGuiFrame(None)
guiFrame = frame
#c = ChildThread(myframe=frame)
pyClass = PyClass(frame)

#show the frame
frame.Show(True)
#start the application
app.MainLoop()
