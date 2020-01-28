"""
 A basic example of how to define a instrument driver.
"""

from PythonTap import *
from System import Double, String
import OpenTap

@Attribute(OpenTap.DisplayAttribute, "Basic Instrument", "A basic example of an instrument driver.", "Python Example")
class BasicInstrument(Instrument):
    def __init__(self):
        "Set up the properties, methods and default values of the instrument."
        super(BasicInstrument, self).__init__() # The base class initializer must be invoked.
        self.count = 0;
        cable_loss = self.AddProperty("cable_loss", 3, Double);
        cable_loss.AddAttribute(OpenTap.UnitAttribute, "dB")
        cable_loss.AddAttribute(OpenTap.DisplayAttribute, "Cable Loss", "Cable loss of the instrument/DUT RF connection.")

        ip_address = self.AddProperty("ip_address", "127.0.0.1", String)
        ip_address.AddAttribute(OpenTap.DisplayAttribute, "IP Address", "The IP Address of the instrument.")

        self.Name = "PyInstrument"
        self.RegisterMethod("do_measurement", Double); #Expose do_measurement in the API.

    def do_measurement(self):
        """Example of a measurement method."""
        self.count = self.count + 1
        print("Measuring " + str(self.count))
        return math.sin(self.count * 4.31532)  - self.cable_loss

    def Open(self):
        """Called by TAP when the test plan starts."""
        self.Info("Python Instrument Opened")

    def Close(self):
        """Called by TAP when the test plan ends."""
        self.Info("Python Instrument Closed")