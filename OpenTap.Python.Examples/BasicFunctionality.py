""" Example of how a python class can be written. """
import sys
import opentap
import clr
clr.AddReference("System.Collections")
from System.Collections.Generic import List
from opentap import *

import OpenTap 
import math
from OpenTap import Log, AvailableValues, EnabledIfAttribute

## Import necessary .net APIs
# These represents themselves as regular Python modules but they actually reflect
# .NET libraries.
import System
from System import Array, Double, Byte, Int32, String, Boolean # Import types to reference for generic methods
from System.ComponentModel import Browsable # BrowsableAttribute can be used to hide things from the user.
import System.Xml
from System.Xml.Serialization import XmlIgnore

from .BasicInstrument import BasicInstrument
from .BasicDut import BasicDut
from .BasicSettings import BasicSettings


# Here is how a test step plugin is defined: 

#Use the Display attribute to define how the test step should be presented to the user.
@attribute(OpenTap.Display("Basic Functionality", "A basic example of the most commonly used functionality.", "Python Example"))
#AllowAnyChildAttribute is attribute that allows any child step to attached to this step
@attribute(OpenTap.AllowAnyChild())
class BasicFunctionality(TestStep): # Inheriting from opentap.TestStep causes it to be a test step plugin.
    # Add properties (name, value, C# type)
    
    Frequency = property(Double, 1e9)\
        .add_attribute(OpenTap.Unit("Hz"))\
        .add_attribute(OpenTap.Display("Frequency", "The frequency"))
    Instrument = property(BasicInstrument, None)\
        .add_attribute(OpenTap.Display("Instrument", "The instrument to use in the step.", "Resources"))
    Dut = property(BasicDut, None).add_attribute(OpenTap.Display( "DUT", "The DUT to use in the step.", "Resources"))
    
    # FrequencyIsDefault is a hidden property that stores the information
    # about if the frequency has changed from the default.
    # if that is the case, then the reset button should appear.
    @property(Boolean)
    @attribute(Browsable(False)) # property not visible for the user.
    def FrequencyIsDefault(self):
        return abs(self.Frequency - 1e9) < 0.001

    Selectable = property(Int32, 0)\
        .add_attribute(OpenTap.AvailableValues("Available"))\
        .add_attribute(OpenTap.Display("Selectable", "Values from Available Values can be selected here.", "Selectable"))
    # This property is based on a C# list of items 'List<int>', List<double>, List<string> can also be used.
    Available = property(List[Int32], None)\
        .add_attribute(OpenTap.Display("Available Values", "Select which values are available for 'Selectable'.", "Selectable"))
    
    Logging = property(OpenTap.Enabled[String], None)\
        .add_attribute(OpenTap.Display("Logging", "Path of where the log file will be stored.", "Result Logging", 1))
    Points = property(Int32, 200)\
        .add_attribute(OpenTap.Display("Points", "Number points to store.", "Result Logging", 1))
    

    def __init__(self):
        super(BasicFunctionality, self).__init__() # The base class initializer must be invoked.
        
        # object types should be initialized in the constructor.
        self.Logging = OpenTap.Enabled[String]()
        
        self.Available = List[Int32]()
        self.Available.Add(1)
        self.Available.Add(2)
        self.Available.Add(3)
        self.Available.Add(4) # the backing data behaves as a python list in this case.
        
        # Add validation rules for the property. This makes it possible to tell the user about invalid property values.
        self.Rules.Add(opentap.Rule("Frequency", lambda: self.Frequency >= 0, lambda: '{} Hz is an invalid value. Frequency must not be negative'.format(self.Frequency)))
        self.Rules.Add(opentap.Rule("Frequency", lambda: self.Frequency <= 2e9, 'Frequency cannot be greater than {}.'.format(2e9)))
    
        

        # C# type 'Enabled<T>' allows the users to specify the status (enable/disable) and the value of the property.
        self.Logging.IsEnabled = True
        self.Logging.Value = "C:\\SessionLogs\\"


    #This Reset Frequency method is exposed to the user as a button that can be clicked.
    @attribute(Browsable(True))
    @attribute(OpenTap.EnabledIf("FrequencyIsDefault", False, HideIfDisabled = True))
    @attribute(OpenTap.Display("Reset Frequency", None))
    @method()
    def resetFrequency(self):
        self.Frequency = 1e9

    def Run(self):
        super().Run() ## 3.0: Required for debugging to work. 
        
        # Write some log messages
        self.log.Debug("Frequency: {0}Hz", self.Frequency)
        self.log.Info("Info message")
        self.log.Error("Error message")
        self.log.Warning("Warning Message")
        
        self.log.Info("Lets create some results:")
        for i in range(0, self.Points):
            self.PublishResult("MyResults1", ["X", "Y"], [i, math.sin(i * 0.1 * 0.5) + math.sin(i * 0.15 * 0.5)])
            self.PublishResult("MyResults2", ["X", "Y"], [i, math.sin(i * 0.3 * 0.5) + math.sin(i * 0.2 * 0.5)])

        self.log.Info("Run the child steps.")
        for step in self.EnabledChildSteps:
            self.RunChildStep(step)
        
        # call method on the instrument.
        self.log.Info("Measurement : {0} dBm", self.Instrument.do_measurement())
        
        self.Dut.init_high_power_mode()
        print("High power mode? " + str(self.Dut.HighPowerOn))

        # Using component settings defined in python:
        print("Number of points for fft: " + str(OpenTap.ComponentSettings.GetCurrent(BasicSettings).NumberOfPoints))
        
        # Set verdict
        self.UpgradeVerdict(OpenTap.Verdict.Pass)
        self.Frequency = self.Frequency + 10
        