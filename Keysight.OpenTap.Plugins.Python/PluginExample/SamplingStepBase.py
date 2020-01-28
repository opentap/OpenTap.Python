"""
Sampling parent step for the power analyzer example. 
Note that the step defined here in is supposed to be the base class for other test steps.
"""
import sys
import math
import PythonTap
from PythonTap import *

import System
from System import Double #Import types to reference for generic methods
from System.ComponentModel import BrowsableAttribute

from OpenTap import DisplayAttribute, UnitAttribute

from .PowerAnalyzer import PowerAnalyzer 
import threading
@Attribute(DisplayAttribute, "SamplingStepBase", "An example of a base class for a TestStep", "Python Example")
@Abstract
class SamplingStepBase(TestStep):
    def __init__(self):
        super(SamplingStepBase, self).__init__() # The base class initializer must be invoked.

        self._sampleNo = 0

        prop = self.AddProperty("MeasurementInterval", 0.2, Double)
        prop.AddAttribute(UnitAttribute, "s")
        prop.AddAttribute(DisplayAttribute, "Measure Interval", "The time between measurements", "Measurements", -50)

        prop = self.AddProperty("PowerAnalyzer", None, PowerAnalyzer)
        prop.AddAttribute(DisplayAttribute, "Power Analyzer", "", "Resources", -100)
    
    def Run(self):
        running = True
        def timer_Elapsed():
            if running == False:
                return;
            voltage = self.PowerAnalyzer.MeasureVoltage()
            current = self.PowerAnalyzer.MeasureCurrent()
            
            self.OnSample(voltage, current, self._sampleNo)
            self._sampleNo = self._sampleNo + 1
            threading.Timer(self.MeasurementInterval, timer_Elapsed).start()
        self._sampleNo = 0;

        timer_Elapsed()
        try:
            # Sleep, while the timer thread generates data.
            # Will stop when the charge/discharge gets within margin
            self.WhileSampling();
            self.RunChildSteps(); # If step has child steps.
        finally:
            running = False

    def WhileSampling(self):
        pass

    def OnSample(self, voltage, current, sampleNo):
        pass
