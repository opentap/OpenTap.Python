"""
Simulated scenario of an emulated power analyzer charging a battery and measuring the voltage curve.
"""
import sys
import clr
import math
import opentap
from opentap import *

from System import Array, Double, Byte, Int32
from System.Diagnostics import Stopwatch
import OpenTap
from OpenTap import Log, DisplayAttribute, Display, Output, Unit, OutputAttribute, UnitAttribute
from .SamplingStepBase import SamplingStepBase

#This is how attributes are used:
@attribute(Display(Name="Charge", Description="Simulated scenario of an emulated power analyzer charging a battery and measuring the voltage curve.", Groups= ["Python Example", "Battery Test"]))
class ChargeStep(SamplingStepBase):
    # Properties
    Current = property(Double, 10)\
        .add_attribute(Unit("A"))\
        .add_attribute(Display("Charge Current", "", "Power Supply", -1, True))
    Voltage = property(Double, 0.1)\
        .add_attribute(Unit("V"))\
        .add_attribute(Display(Name="Voltage", Group="Power Supply", Order=0, Collapsed=True))
    TargetCellVoltageMargin = property(Double, 0.1)\
        .add_attribute(Display("Target Voltage Margin", "", "Cell", -1))\
        .add_attribute(Unit("V"))
    ChargeType = property(Double, 0.0)\
        .add_attribute(Unit("s"))\
        .add_attribute(Display("Charge Time", "", "Output", 0))\
        .add_attribute(Output())
        
    def __init__(self):
        super(ChargeStep, self).__init__() # The base class initializer must be invoked.
        
    def Run(self):
        sw = Stopwatch.StartNew()
        self.PowerAnalyzer.Setup(self.Voltage, self.Current)
        self.PowerAnalyzer.EnableOutput()
        self.log.Info("Charging at: " + str(self.Current) + "A" + " Target Voltage: " + str(self.Voltage) + "V")
        super(ChargeStep, self).Run()
        self.PowerAnalyzer.DisableOutput()
        self.ChargeTime = sw.Elapsed.TotalSeconds

    def WhileSampling(self):
        while math.fabs(self.PowerAnalyzer.MeasureVoltage() - self.Voltage) > self.TargetCellVoltageMargin:
            OpenTap.TapThread.Sleep(50)

    def OnSample(self, voltage, current, sampleNo):
        barVoltage = OpenTap.TraceBar()
        barVoltage.LowerLimit = 2
        barVoltage.UpperLimit = 4.7
        self.log.Info("Voltage: " + str(barVoltage.GetBar(voltage)))
        v = math.trunc(voltage * 100) / 100.0
        c = math.trunc(current * 100) / 100.0
        self.PublishResult("Charge", ["Sample Number", "Voltage", "Current"], [sampleNo, v, c])
