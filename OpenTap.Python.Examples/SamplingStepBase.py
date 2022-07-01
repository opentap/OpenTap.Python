"""
Sampling parent step for the power analyzer example. 
Note that the step defined here in is supposed to be the base class for other test steps.
"""
import sys
import math
import opentap
from opentap import *

import System
from System import Double #Import types to reference for generic methods
from System.ComponentModel import BrowsableAttribute

from OpenTap import DisplayAttribute, UnitAttribute

from .PowerAnalyzer import PowerAnalyzer 
import threading
@attribute(DisplayAttribute, "SamplingStepBase", "An example of a base class for a TestStep", "Python Example")
class SamplingStepBase(TestStep):
    __clr_abstract__ = True #class marked 'abstract' as it should not be used without inheritance
    MeasurementInterval = property(Double, 0.2)\
        .add_attribute(UnitAttribute, "s")\
        .add_attribute(DisplayAttribute, "Measure Interval", "The time between measurements", "Measurements", -50)
    PowerAnalyzer = property(PowerAnalyzer, None)\
        .add_attribute(DisplayAttribute, "Power Analyzer", "", "Resources", -100)
    def __init__(self):
        super(SamplingStepBase, self).__init__() # The base class initializer must be invoked.
        self._sampleNo = 0

    def Run(self):
        running = True
        def timer_Elapsed():
            if running == False:
                return
            voltage = self.PowerAnalyzer.MeasureVoltage()
            current = self.PowerAnalyzer.MeasureCurrent()
            
            self.OnSample(voltage, current, self._sampleNo)
            self._sampleNo = self._sampleNo + 1
            threading.Timer(self.MeasurementInterval, timer_Elapsed).start()
        self._sampleNo = 0

        timer_Elapsed()
        try:
            # Sleep, while the timer thread generates data.
            # Will stop when the charge/discharge gets within margin
            self.WhileSampling()
            self.RunChildSteps(); # If step has child steps.
        finally:
            running = False

    def WhileSampling(self):
        pass

    def OnSample(self, voltage, current, sampleNo):
        pass
