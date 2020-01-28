"""
Simulated scenario of discharging a battery while a power analyzer is measuring the voltage curve.
"""
import sys
import clr
import math
import PythonTap
from PythonTap import *

from System import Array, Double, Byte, Int32 #Import types to reference for generic methods
from System.Diagnostics import Stopwatch

import OpenTap
import Keysight.OpenTap.Plugins.Python
from OpenTap import Log, DisplayAttribute, OutputAttribute, UnitAttribute

from .SamplingStepBase import SamplingStepBase

@Attribute(DisplayAttribute, Name="Discharge", Description="Simulated scenario of discharging a battery while a power analyzer is measuring the voltage curve.", Groups= ["Python Example", "Battery Test"])
class DischargeStep(SamplingStepBase):
    def __init__(self):
        super(DischargeStep, self).__init__() # The base class initializer must be invoked.
        prop = self.AddProperty("Current", 5, Double);
        prop.AddAttribute(UnitAttribute, "A")
        prop.AddAttribute(DisplayAttribute, "Discharge Current", "", "Power Supply", -1, True);

        prop = self.AddProperty("Voltage", 2.2, Double);
        prop.AddAttribute(UnitAttribute, "V")
        prop.AddAttribute(DisplayAttribute, Name="Voltage", Group="Power Supply", Order=0, Collapsed=True)

        prop = self.AddProperty("TargetCellVoltageMargin", 0.8, Double);
        prop.AddAttribute(UnitAttribute, "V")
        prop.AddAttribute(DisplayAttribute, "Target Voltage Margin", "", "Cell", -1)

        prop = self.AddProperty("DischargeTime", 0.0, Double);
        prop.AddAttribute(UnitAttribute, "s")
        prop.AddAttribute(DisplayAttribute, "Discharge Time", "", "Output", 0)
        prop.AddAttribute(OutputAttribute)
        
    def Run(self):
        sw = Stopwatch.StartNew()
        self.PowerAnalyzer.Setup(self.Voltage, self.Current)
        self.PowerAnalyzer.EnableOutput()
        self.Info("Discharging at: " + str(self.Current) + "A" + " Target Voltage: " + str(self.Voltage) + "V");
        super(DischargeStep, self).Run()
        self.PowerAnalyzer.DisableOutput()
        self.DischargeTime = sw.Elapsed.TotalSeconds

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
        self.PublishResult("Discharge", ["Sample Number", "Voltage", "Current"], [sampleNo, v, c]);
