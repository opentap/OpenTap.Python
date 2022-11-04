from opentap import *
from System import Double, String
import OpenTap

@attribute(OpenTap.Display("Basic SCPI Instrument", "A basic example of a SCPI instrument driver.", "Python Example"))
class BasicScpiInstrument(OpenTap.ScpiInstrument):
    
    def __init__(self):
        super(BasicScpiInstrument, self).__init__()
        self.log = Trace(self)
    
    def GetIdnString(self):
        self.ScpiQuery[String]("*IDN?")
    
    def SetFrequency(self, frequency):
        self.ScpiCommand("CENT:FREQ {0}", frequency)
        
    def GetFrequency(self):
        self.ScpiQuery[Double]("CENT:FREQ {0}")