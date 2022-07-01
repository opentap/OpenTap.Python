"""
Example of using enums. Enums are classes with a defined selection of values. Hence in graphical user interfaces they can often be shown as drop-downs.
Enums can be annotated with special names and desriptions than makes it easier to describe what they represent.
"""
import opentap
import clr
clr.AddReference("System.Collections")
from opentap import *

from OpenTap.Cli import *
import OpenTap
from enum import Enum

class Color1(Enum):
    RED = ("Red color", "This is red color")
    GREEN   = ("Green color", "This is green color")
    BLUE   = ("Blue color", "This is blue color")

@attribute(DisplayAttribute, "Example Python Enum with enumeration", "An example of Python enum with enumeration member", "Python Example")
class Color2(Enum):
    RED = 1
    GREEN = 2
    BLUE = 3

@attribute(DisplayAttribute, "Example Python Enum without enumeration", "An example of Python enum without enumeration member", "Python Example")
class Color3(AutoNumber):
    RED = ()
    GREEN = ()
    BLUE = ()

class InputImpedanceEnum(Enum):
    TenMegaOhm = ("10 MOhm", "Fixed 10MOhm")
    HighZ = ("High Z", "High impedance")
    UserSpecify = ("User specified", "Input impedance is specified by User")

@attribute(DisplayAttribute, "Enum Usage", "An example of how to use enums.", "Python Example")
class EnumUsage(TestStep):
    InputImpedance = property(InputImpedanceEnum, InputImpedanceEnum.TenMegaOhm)\
        .add_attribute(OpenTap.DisplayAttribute, "Input Impedance"
    def __init__(self):
        super(EnumUsage, self).__init__() # The base class initializer must be invoked.

        self.AddProperty("SelectedVerdict", OpenTap.Verdict.Pass, OpenTap.Verdict)\
            .add_attribute(OpenTap.DisplayAttribute, "Selected Verdict", "The verdict that this step will have when it is finished. It uses the common OpenTAP.Verdict enum.")
        self.AddProperty("LogEvent", OpenTap.LogEventType.Error, OpenTap.LogEventType)\
            .add_attribute(OpenTap.DisplayAttribute, "Log Event", "A log event type. This is the common OpenTAP.LogEventType enum.")        
        self.AddProperty("ColorOne", Color1.RED, Color1)
            .add_attribute(OpenTap.DisplayAttribute, "Color One")
        self.AddProperty("ColorTwo", Color2.RED, Color2)\
            .add_attribute(OpenTap.DisplayAttribute, "Color Two")
        self.AddProperty("ColorThree", Color3.RED, Color3)\
            .add_attribute(OpenTap.DisplayAttribute, "Color Three")

    def Run(self):
        self.UpgradeVerdict(self.SelectedVerdict)
        print(self.InputImpedance);
        print(self.LogEvent);