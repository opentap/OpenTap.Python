import clr
import os
import sys
import time
import abc
tapPath = os.environ["TAP_PATH"]
sys.path.append(tapPath)
print clr.__file__

clr.AddReference("OpenTap")
#clr.AddReference("MyLibrary")
clr.AddReference("Keysight.OpenTap.Plugins.BasicSteps")
clr.AddReference("System")
clr.AddReference("Keysight.OpenTap.Plugins.Python.PyHelper")
from OpenTap import *
from Keysight.OpenTap.Plugins.BasicSteps import *
from abc import *
#from MyLibrary import *
import time

from Keysight.OpenTap.Plugins.Python import *
from System.Collections.Generic import List

##class MyNewResultListener ( MyResultListener ):
##  def __init__(self):
##    print "Hello from Result Listener class"
##  def OnResultPublished(self, stepRunId, result):
##    print "Hello1"
##  def OnTestPlanRunCompleted(self, planRun, logStream):
##    print "Hello2"
##  def OnTestPlanRunStart(self, planRun):
##    print "Hello3"
##  def OnTestStepRunCompleted(self, stepRun):
##    print "Hello4"
##  def OnTestStepRunStart(self, stepRun):
##    print "Hello5"


##for plugin in PluginManager.GetAllPlugins():
##    print(plugin.Name)

# If you have plugins in directories different from the location of Engine.dll, then add those directories here.
# PluginManager.DirectoriesToSearch.Add(@"C:\SomeOtherDirectory");

# Required to find plugins
#print "Test Plan Started"
PluginManager.Search()

SessionLogs.Load("console_log.txt")

myTestPlan = TestPlan()
mySequenceStep = SequenceStep()

myTestPlan.Name = "MyTestplan"
myDelayStep1 = DelayStep()
myDelayStep1.DelaySecs = 2.5
myDelayStep1.Name = "Delay1"

myDelayStep2 = DelayStep()
myDelayStep2.DelaySecs = 1.1
myDelayStep2.Name = "Delay2"

myVerdictStep = VerdictStep()
myVerdictStep.Name = "Verdict"
myVerdictStep.Verdict = Verdict.Pass

mySequenceStep.ChildTestSteps.Add(myDelayStep1)
mySequenceStep.ChildTestSteps.Add(myDelayStep2)
mySequenceStep.ChildTestSteps.Add(myVerdictStep)
myTestPlan.ChildTestSteps.Add(mySequenceStep)

myTestPlan.Save(tapPath + "\\" + "MyTestPlan.TapPlan")

#myResultListener = MyNewResultListener()

myResultListener = PyHelper.ResultListener()

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
  print stepRun.TestStepName, "started"
  pass
#  print "TestStepRunStart"
def PyClose():
  pass
#  print "Close"

myResultListener.WOpen += PyOpen

myResultListener.WOnResultPublished += PyOnResultPublished

myResultListener.WOnTestStepRunStart += PyOnTestStepRunStart
myResultListener.WOnTestStepRunCompleted += PyOnTestStepRunCompleted

myResultListener.WOnTestPlanRunStart += PyOnTestPlanRunStart
myResultListener.WOnTestPlanRunCompleted += PyOnTestPlanRunCompleted

myResultListener.WClose += PyClose

myList = List[PyHelper.ResultListener]()

myList.Add(myResultListener)

#myTestPlan.Execute(myList)

##myTestPlan.Execute()
#myTestPlan.Execute(myResultListener)

#print "Test Plan Ended"
