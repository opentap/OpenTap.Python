import clr
import os
import sys
import time
import abc
tapPath = os.environ["TAP_PATH"]
sys.path.append(tapPath)
print clr.__file__

clr.AddReference("Keysight.Tap.Engine")
clr.AddReference("MyLibrary")
clr.AddReference("Keysight.Tap.Plugins.BasicSteps")
clr.AddReference("System")
from System.IO import FileStream
from System.IO import FileMode
from System import TimeSpan
from OpenTap import *
from Keysight.OpenTap.Plugins.BasicSteps import *
from abc import *
from MyLibrary import *

class MyNewClass ( MyClass ):
  def MyFunc(self):
    return super(MyNewClass, self).MyFunc() + 1

class MyNewAbstractClass ( MyAbstractClass ):
  def MyFunc2(self):
    return super(MyNewAbstractClass, self).MyFunc2()

class MyResultListener ( ResultListener ):
  def __init__(self):
    print "Hello"
  def OnResultPublished(self, stepRunId, result):
    print "Hello1"
  def OnTestPlanRunCompleted(self, planRun, logStream):
    print "Hello2"
  def OnTestPlanRunStart(self, planRun):
    print "Hello3"
  def OnTestStepRunCompleted(self, stepRun):
    print "Hello4"
  def OnTestStepRunStart(self, stepRun):
    print "Hello5"


for plugin in PluginManager.GetAllPlugins():
    print(plugin.Name)

myTest = MyNewClass()
print "My Value:"
myVar = myTest.MyFunc()
print myVar

myTest2 = MyNewAbstractClass()
print "My Value: "
myVar2 = myTest2.MyFunc2()
print myVar2

# If you have plugins in directories different from the location of Engine.dll, then add those directories here.
# PluginManager.DirectoriesToSearch.Add(@"C:\SomeOtherDirectory");

# Required to find plugins
print "Test Plan Started"
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

mySequenceStep.ChildTestSteps.Add(myDelayStep1)
mySequenceStep.ChildTestSteps.Add(myDelayStep2)
myTestPlan.ChildTestSteps.Add(mySequenceStep)

myTestPlan.Save(tapPath + "\\" + "MyTestPlan.TapPlan")

myResultListener = MyResultListener()

myTestPlan.Execute(myResultListener)

print "Test Plan Ended"
