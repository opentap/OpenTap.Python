"""
Simulated Power Analyzer example.

This power analyzer simulation simulates charging and discharging a battery and measuring the voltage meanwhile.

The instrument plugin created by this example is accessible from a .NET API by referencing the built example directly.
From a .NET point of view, the assembly is called Python.PluginExample.dll and the instrument is named Python.PluginExample.PowerAnalyzer.
"""
import opentap
from opentap import *
from System import Double, Random #Import types to reference for generic methods
from System.Diagnostics import Stopwatch
import OpenTap
from OpenTap import DisplayAttribute

@attribute(DisplayAttribute, "Power Analyzer", "Simulated power analyzer instrument used for charge/discharge demo steps written in python.", "Python Example")
class PowerAnalyzer(Instrument):
    CellSizeFactor = property(Double, 0.005)\
        .add_attribute(DisplayAttribute, "Cell Size Factor", "A larger cell size will result in faster charging and discharging.")
    def __init__(self):
        super().__init__() # The base class initializer must be invoked.
        self._voltage = 1.0
        self._cellVoltage = 2.7
        self._current = 10
        self._currentLimit = 0.0
        self._sw = None
        self.Name = "PyPowerAnalyzer"

    def Open(self):
        super().Open()
        self._voltage = 0
        self._cellVoltage = 2.7

    def Close(self):
        if self._sw != None:
            self._sw.Stop()
        super().Close()
    @method(Double)
    def MeasureCurrent(self):
        self.UpdateCurrentAndVoltage()
        return self._current

    @method(Double)
    def MeasureVoltage(self):
        self.UpdateCurrentAndVoltage()
        return self._cellVoltage
    @method(None, [Double, Double])
    def Setup(self, voltage, current):
        self._voltage = voltage
        self._currentLimit = current
        self._current = current
    @method()
    def EnableOutput(self):
        if self._sw == None or self._sw.IsRunning == False:
            self._sw = Stopwatch.StartNew()
    @method()
    def DisableOutput(self):
        if self._sw != None:
            self._sw.Stop()

    def UpdateCurrentAndVoltage(self):
        if self._sw == None or self._sw.IsRunning == False:
            return

        # Generates a somewhat random curve that gradually approaches the limit.
        self._current = self._currentLimit * ((self._voltage - self._cellVoltage) * 2) + Random().NextDouble() * self._currentLimit / 50.0;

        if self._current >= self._currentLimit:
            self._current = self._currentLimit;
        elif self._current < 0 - self._currentLimit:
            self._current = 0 - self._currentLimit;

        self._cellVoltage += self.CellSizeFactor * self._current * self._sw.Elapsed.TotalSeconds * 10;
        self._sw.Restart();

