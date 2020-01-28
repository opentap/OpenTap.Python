"""
Simulated scenario of an emulated power analyzer charging a battery and measuring the voltage curve.
"""
import sys
import clr
import math
import PythonTap
from PythonTap import *

from System import Array, Double, Byte, Int32
from System.Diagnostics import Stopwatch

import OpenTap
import Keysight.OpenTap.Plugins.Python
from OpenTap import Log, DisplayAttribute, OutputAttribute, UnitAttribute

from .SamplingStepBase import SamplingStepBase

#This is how attributes are used:
@Attribute(DisplayAttribute, Name="Charge", Description="Simulated scenario of an emulated power analyzer charging a battery and measuring the voltage curve.", Groups= ["Python Example", "Battery Test"])
class ChargeStep(SamplingStepBase):
    def __init__(self):
        super(ChargeStep, self).__init__() # The base class initializer must be invoked.
        prop = self.AddProperty("Current", 10, Double);
        prop.AddAttribute(UnitAttribute, "A")
        prop.AddAttribute(DisplayAttribute, "Charge Current", "", "Power Supply", -1, True);

        prop = self.AddProperty("Voltage", 4.2, Double);
        prop.AddAttribute(UnitAttribute, "V")
        prop.AddAttribute(DisplayAttribute, Name="Voltage", Group="Power Supply", Order=0, Collapsed=True)

        prop = self.AddProperty("TargetCellVoltageMargin", 0.1, Double);
        prop.AddAttribute(UnitAttribute, "V")
        prop.AddAttribute(DisplayAttribute, "Target Voltage Margin", "", "Cell", -1)

        prop = self.AddProperty("ChargeTime", 0.0, Double);
        prop.AddAttribute(UnitAttribute, "s")
        prop.AddAttribute(DisplayAttribute, "Charge Time", "", "Output", 0)
        prop.AddAttribute(OutputAttribute)

        # Rules go here
        
    def Run(self):
        sw = Stopwatch.StartNew()
        self.PowerAnalyzer.Setup(self.Voltage, self.Current)
        self.PowerAnalyzer.EnableOutput()
        self.Info("Charging at: " + str(self.Current) + "A" + " Target Voltage: " + str(self.Voltage) + "V");
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
        self.Info("Voltage: " + str(barVoltage.GetBar(voltage)))
        v = math.trunc(voltage * 100) / 100.0
        c = math.trunc(current * 100) / 100.0
        self.PublishResult("Charge", ["Sample Number", "Voltage", "Current"], [sampleNo, v, c]);
