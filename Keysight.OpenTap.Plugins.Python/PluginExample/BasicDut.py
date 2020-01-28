"""
 A basic example of how to define a DUT driver.
"""
from PythonTap import *
import OpenTap

@Attribute(OpenTap.DisplayAttribute, "Basic DUT", "A basic example of a DUT driver.", "Python Example")
class BasicDut(Dut):
    def __init__(self):
        super(BasicDut, self).__init__() # The base class initializer must be invoked.
        self.AddProperty("Firmware", "1.0.2", None).AddAttribute(OpenTap.DisplayAttribute, "Firmware Version", "The firmware version of the DUT.", "Common")
        self.HighPowerOn = False
        self.Name = "PyDUT"

    def init_high_power_mode(self):
        """Example of a method invoked on the DUT. This could send an AT command to the DUT."""
        self.HighPowerOn = True

    def Open(self):
        """Called by TAP when the test plan starts."""
        self.Info(self.Name + " Opened")

    def Close(self):
        """Called by TAP when the test plan ends."""
        self.Info(self.Name + " Closed")
        self.HighPowerOn = False