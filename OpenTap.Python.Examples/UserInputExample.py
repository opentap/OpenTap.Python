"""
 A basic example of how to use User Input.
"""
from System import String, Object, Double
import System.Threading
import OpenTap
from OpenTap import Display
from opentap import *

class BasicUserInput(Object): # Must inherit from _some_ .NET type, for example Object
   Frequency = property(Double, 1.0).add_attribute(Display("Frequency", "The selected frequency."))
   def __init__(self):
      super().__init__()

@attribute(OpenTap.Display("Basic User Input Step", "An example of asking for user input.", "Python Example"))
class BasicUserInputStep(TestStep):
   def __init__(self):
      super().__init__()
   def Run(self):
      super().Run()
      
      obj = BasicUserInput()
      # This should pop up a dialog asking the user to fill out the data in the object.
      OpenTap.UserInput.Request(obj)
      print(obj.Frequency)  
