from opentap import *
from System import Int32
import OpenTap

@attribute(OpenTap.Display("Test Step Example", "A simple python test step", "Python Project Template"))
class TestStepExample (TestStep):
    Number = property(Int32, 1000)
    def __init__(self):
        super(TestStepExample, self).__init__()
    
    def Run(self):
        self.log.Debug("Number: {0}", Int32(self.Number))
   