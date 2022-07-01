"""
 A basic example of how to define a DUT driver.
"""
from opentap import *
import System
from System import String
@attribute(OpenTap.Display("Basic DUT", "A basic example of a DUT driver.", "Python Example"))
class BasicDut(Dut):
    # Add a Firmware version setting which can be configured by the user.
    Firmware = property(String, "1.0.2")\
        .add_attribute(OpenTap.Display( "Firmware Version", "The firmware version of the DUT.", "Common"))
    def __init__(self):
        super(BasicDut, self).__init__() # The base class initializer must be invoked.
        self.HighPowerOn = False
        self.Name = "PyDUT"

    def init_high_power_mode(self):
        """Example of a method invoked on the DUT. This could send an AT command to the DUT."""
        self.HighPowerOn = True

    def Open(self):
        """Called by TAP when the test plan starts."""
        self.log.Info(self.Name + " Opened")

    def Close(self):
        """Called by TAP when the test plan ends."""
        self.log.Info(self.Name + " Closed")
        self.HighPowerOn = False