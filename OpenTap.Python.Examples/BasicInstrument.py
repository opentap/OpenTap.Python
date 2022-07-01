"""
 A basic example of how to define a instrument driver.
"""

from opentap import *
from System import Double, String
import OpenTap

@attribute(OpenTap.Display("Basic Instrument", "A basic example of an instrument driver.", "Python Example"))
class BasicInstrument(Instrument):
    CableLoss = property(Double, 3.0)\
        .add_attribute(OpenTap.Unit("dB"))\
        .add_attribute(OpenTap.Display("Cable Loss", "Cable loss of the instrument/DUT RF connection."))
    IpAddress = property(String, "127.0.0.1")\
        .add_attribute(OpenTap.Display("IP Address", "The IP Address of the instrument."))
    def __init__(self):
        "Set up the properties, methods and default values of the instrument."
        super(BasicInstrument, self).__init__() # The base class initializer must be invoked.
        self.count = 0
        self.Name = "PyInstrument"
         
    #Expose do_measurement in the API.
    @method(Double)
    def do_measurement(self):
        """Example of a measurement method."""
        self.count = self.count + 1
        print("Measuring " + str(self.count))
        return math.sin(self.count * 4.31532)  - self.CableLoss

    def Open(self):
        """Called by TAP when the test plan starts."""
        self.log.Info("Python Instrument Opened")

    def Close(self):
        """Called by TAP when the test plan ends."""
        self.log.Info("Python Instrument Closed")