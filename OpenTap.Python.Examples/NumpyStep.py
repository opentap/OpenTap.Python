from opentap import *
import numpy
import System

@attribute(OpenTap.Display("Numpy Step", "An example of using numpy to generate results.", "Python Example"))
class NumpyStep(TestStep):
   Points = property(System.Int32, 32)
   def __init__(self):
      super().__init__()
   def Run(self):
      X = numpy.arange(self.Points)
      Y = numpy.sin(X)
      
