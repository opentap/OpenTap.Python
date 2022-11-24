"""
Example of using enums. Enums are classes with a defined selection of values. Hence in graphical user interfaces they can often be shown as drop-downs.
Enums can be annotated with special names and desriptions than makes it easier to describe what they represent.
"""
from opentap import *
from OpenTap.Cli import *
import OpenTap
from OpenTap import Display
from enum import Enum

class Color1(Enum):
    RED = ("Red color", "This is red color")
    GREEN = ("Green color", "This is green color")
    BLUE = ("Blue color", "This is blue color")
    
    # The following two methods defines how an enum will present itself to the user. 
    def __str__(self):
        return self.value[0]
    def describe(self):
            return self.value[1]

@attribute(Display("Example Python Enum with enumeration", "An example of Python enum with enumeration member", "Python Example"))
class Color2(Enum):
    RED = 1
    GREEN = 2
    BLUE = 3

@attribute(Display("Example Python Enum without enumeration", "An example of Python enum without enumeration member", "Python Example"))
class Color3(Enum):
    RED = 5
    GREEN = 6
    BLUE = 7
    "Instead of 'Color3.RED', just print 'RED'"
    def __str__(self):
        return self.name

class InputImpedanceEnum(Enum):
    TenMegaOhm = ("10 MOhm", "Fixed 10MOhm")
    HighZ = ("High Z", "High impedance")
    UserSpecify = ("User specified", "Input impedance is specified by User")
    
    def __str__(self):
        return self.value[0]
    
    def describe(self):
        return self.value[1]

@attribute(Display("Enum Usage", "An example of how to use enums.", "Python Example"))
class EnumUsage(TestStep):
    InputImpedance = property(InputImpedanceEnum, InputImpedanceEnum.TenMegaOhm)\
        .add_attribute(Display("Input Impedance"))
    SelectedVerdict = property(OpenTap.Verdict, OpenTap.Verdict.Pass)
    ColorOne = property(Color1, Color1.RED)
    ColorTwo = property(Color2, Color2.RED)
    ColorThree = property(Color3, Color3.RED)
    LogEvent = property(OpenTap.LogEventType, OpenTap.LogEventType.Error)
    
    def __init__(self):
        super(EnumUsage, self).__init__() # The base class initializer must be invoked.

    def Run(self):
        self.UpgradeVerdict(self.SelectedVerdict)
        print(self.InputImpedance)
        print(self.LogEvent)