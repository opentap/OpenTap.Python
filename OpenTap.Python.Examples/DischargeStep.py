"""
Simulated scenario of discharging a battery while a power analyzer is measuring the voltage curve.
"""
import sys
import clr
import math
import opentap
from opentap import *

from System import Array, Double, Byte, Int32 #Import types to reference for generic methods
from System.Diagnostics import Stopwatch

import OpenTap
from OpenTap import Log, Display, DisplayAttribute, Output, Unit, UnitAttribute, OutputAttribute

from .SamplingStepBase import SamplingStepBase

@attribute(DisplayAttribute, "Discharge", Description="Simulated scenario of discharging a battery while a power analyzer is measuring the voltage curve.", Groups= ["Python Example", "Battery Test"])
class DischargeStep(SamplingStepBase):
    Current = property(Double, 5.0)\
        .add_attribute(Unit("A"))\
        .add_attribute(Display("Discharge Current", "", "Power Supply", -1, True))
    Voltage = property(Double, 2.2)\
        .add_attribute(Unit("V"))\
        .add_attribute(Display("Voltage", Group="Power Supply", Order=0, Collapsed=True))
    TargetCellVoltageMargin = property(Double, 0.8)\
        .add_attribute(UnitAttribute, "V")\
        .add_attribute(DisplayAttribute, "Target Voltage Margin", "", "Cell", -1)
    DischargeTime = property(Double, 0.0)\
        .add_attribute(UnitAttribute, "s")\
        .add_attribute(DisplayAttribute, "Discharge Time", "", "Output", 0)\
        .add_attribute(OutputAttribute)
    def __init__(self):
        super().__init__() # The base class initializer must be invoked.
        
    def Run(self):
        sw = Stopwatch.StartNew()
        self.PowerAnalyzer.Setup(self.Voltage, self.Current)
        self.PowerAnalyzer.EnableOutput()
        self.log.Info("Discharging at: " + str(self.Current) + "A" + " Target Voltage: " + str(self.Voltage) + "V");
        super().Run()
        self.PowerAnalyzer.DisableOutput()
        self.DischargeTime = sw.Elapsed.TotalSeconds

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
        self.PublishResult("Discharge", ["Sample Number", "Voltage", "Current"], [sampleNo, v, c]);
