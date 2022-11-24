"""
 A basic example of how to use User Input.
"""
from System import String, Object, Double
import System.Threading
import OpenTap
from OpenTap import Display, Submit, Layout, LayoutMode
from opentap import *
from enum import Enum

# This adds a couple buttons when the user request is invoked. Click OK, or cancel...
class OkEnum(Enum):
    Ok = ("Ok", "Ok")
    Cancel = ("Cancel", "Cancel")

    def __str__(self):
        return self.value[0]
    def describe(self):
        return self.value[1]

# Notice, this class inherits from System.Object(see line 4), a .NET class, not the default python object class.
class BasicUserInput(Object):
   Frequency = property(Double, 1.0).add_attribute(Display("Frequency", "The selected frequency."))
   Ok = property(OkEnum, OkEnum.Ok)\
        .add_attribute(Submit())\
        .add_attribute(Layout(LayoutMode.FullRow | LayoutMode.FloatBottom))
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
