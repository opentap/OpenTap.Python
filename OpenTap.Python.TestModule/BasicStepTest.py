""" Example of how a python class can be written. """
import sys
import opentap
from opentap import *
import clr
import OpenTap
from OpenTap import *
import math
from OpenTap import Log, AvailableValues, EnabledIf, Display, EmbedProperties
import System
from System import Array, Double, Byte, Int32, String, Boolean, Void
from System.ComponentModel import Browsable
import OpenTap.Python

class DutTest(OpenTap.Dut):
    __namespace__ = "TestModule"
    def __init__(self):
        super(DutTest, self).__init__()
    Frequency = clr.property(Double, 1e9)

class BasicStepTest(opentap.TestStep):
    __clr_attribute__ = [Display("Basic Step Test")]
    __namespace__ = "TestModule"
    def __init__(self):
        print("BasicStepTestInit!")
        super().__init__()
        
    Frequency = property(Double, 1e9) #clr.clrproperty(Double, get_Frequency, set_Frequency)
    Frequency2 = property(Double, 10.0)\
        .add_attribute(OpenTap.Display("Frequency"))\
        .add_attribute(OpenTap.Unit("Hz"))
    PrePlanRunExecuted = property(Boolean, False)
    PostPlanRunExecuted = property(Boolean, False)
    
    Dut = property(DutTest, None)
    
    def PrePlanRun(self):
        super(BasicStepTest, self).PrePlanRun()
        self.PrePlanRunExecuted = True
        self.log.Debug("Overridden pre plan run!")
    def PostPlanRun(self):
        self.PostPlanRunExecuted = True
        self.log.Debug("Overridden post plan run!")
        
    def Run(self):
        super(BasicStepTest, self).Run()
        self.Frequency = self.Frequency + 10
        self.log.Debug("Frequency: {0}Hz", self.Frequency)
        self.log.Debug("Frequency2: {0}Hz", self.Frequency2)
        self.log.Info("Info message")
        self.log.Error("Error message")
        self.log.Warning("Warning Message")
        self.log.Debug("DUT: {0}", self.Dut)
    @method(attributes = [Browsable(True), Display("Method Test")])
    def MethodTest(self):
        self.log.Info("Hello from method")
        
class ResultsStepTest(opentap.TestStep):
    __namespace__ = "TestModule"
    
    def Run(self):
        super().Run()
        if self.Results == None:
            raise NameError("Results not defined!!")
            
class BasicStep2Test(opentap.TestStep):
    __clr_attributes__ = [Display("BasicStep2 Test")]
    __namespace__ = "TestModule"
    Frequency = property(Double, 1.0)
    def __init__(self):
        super(BasicStep2Test, self).__init__()
    def Run(self):
        super(BasicStep2Test, self).Run()
        self.log.Info("Frequeny was {0}", self.Frequency)
        
class StepWithNoCtor(opentap.TestStep):
    __clr_attributes__ = [Display("No Constructor test")]
    __namespace__ = "TestModule"
    Frequency = property(Double, 1.0)
    def Run(self):
        super(StepWithNoCtor, self).Run()
        self.log.Info("Frequeny was {0}", self.Frequency)
        
# see https://github.com/pythonnet/pythonnet/issues/1774
class StepWithNoNamespace(opentap.TestStep):
    __clr_attributes__ = [Display("No Namespace test")]
    Frequency = property(Double, 1.0)
    def Run(self):
        super(StepWithNoNamespace, self).Run()
        self.log.Info("Frequency was {0}", self.Frequency)
        
class TestScpiInstrument(OpenTap.ScpiInstrument):
    __namespace__ = "Test"
    def __init__(self):
        print("SCPI Instrument init")
        super().__init__()
        print("SCPI Instrument init done!")
    
class TestScpiInstrument2(OpenTap.ScpiInstrument):
    def __init__(self):
        print("SCPI Instrument init")
        super().__init__()
        print("SCPI Instrument init done!")

class TestStep2(OpenTap.RfConnection):
    A = property(Double, 1.0)
    
class LockManager(OpenTap.ILockManager):
    def BeforeOpen(self, resources, abortToken):
        pass

    def AfterClose(self, resources, abortToken):
        pass

class LockManager2(OpenTap.ILockManager):
    __namespace__ = "Test"
    def BeforeOpen(self, resources, abortToken):
        pass

    def AfterClose(self, resources, abortToken):
        pass

class SettingsTest(OpenTap.ComponentSettings):
    __clr_attributes__ = [Display("Settings Test")]
    A = property(Double, 1.0)
    B = property(Double, 1.0)
    
class NestedType(System.Object):
    A = property(Double, 1.0)

class TestStep3(opentap.TestStep):
    __namespace__ = "Test"
    B = property(NestedType, None).add_attribute(EmbedProperties())
    C = property(Double, 1.0)
        
    def __init__(self):
        super().__init__()
        self.B = NestedType()
    def Run(self):
        assert self.C == self.B.A