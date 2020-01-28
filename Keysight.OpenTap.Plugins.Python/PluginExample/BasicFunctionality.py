"""
Example of how a python class can be written to work with TAP.
"""
import sys
import PythonTap
import clr
clr.AddReference("System.Collections")
from System.Collections.Generic import List
from PythonTap import *

import OpenTap
import math
from OpenTap import Log, AvailableValuesAttribute, EnabledIfAttribute
from System import Array, Double, Byte, Int32, String, Boolean # Import types to reference for generic methods
import System
from System.ComponentModel import BrowsableAttribute # BrowsableAttribute can be used to hide things from the user.

from .BasicInstrument import BasicInstrument
from .BasicDut import BasicDut
from .BasicSettings import BasicSettings

import System.Xml
from System.Xml.Serialization import XmlIgnoreAttribute

#This is how attributes are used:
@Attribute(OpenTap.DisplayAttribute, "Basic Functionality", "A basic example of the most commonly used functionality.", "Python Example")
#AllowAnyChildAttribute is attribute that allows any child step to attached to this step
@Attribute(OpenTap.AllowAnyChildAttribute)
# Here is how a test step plugin is defined: 
class BasicFunctionality(TestStep): # Inheriting from PythonTap.TestStep causes it to be a test step plugin.
    def __init__(self):
        super(BasicFunctionality, self).__init__() # The base class initializer must be invoked.

        # Add properties (name, value, C# type)
        prop = self.AddProperty("Frequency", 1e9, Double)
        prop.AddAttribute(OpenTap.UnitAttribute, "Hz") #Property attributes can be added like this.
        prop.AddAttribute(OpenTap.DisplayAttribute, "Frequency", "The frequency")

        # Add validation rules for the property. This makes it possible to tell the user about invalid property values.
        self.AddRule(lambda: self.Frequency >= 0, lambda: '{} Hz is an invalid value. Frequency must not be negative'.format(self.Frequency), "Frequency")
        self.AddRule(lambda: self.Frequency <= 2e9, 'Frequency cannot be greater than {}.'.format(2e9), "Frequency")

        self.AddProperty("Instrument", None, BasicInstrument).AddAttribute(OpenTap.DisplayAttribute, "Instrument", "The instrument to use in the step.", "Resources")
        self.AddProperty("Dut", None, BasicDut).AddAttribute(OpenTap.DisplayAttribute, "DUT", "The DUT to use in the step.", "Resources")
        
        # FrequencyIsDefault is a hidden property that stores the information
        # about if the frequency has changed from the default.
        # if that is the case, then the reset button should appear.
        frequencyIsDefault = self.AddProperty("FrequencyIsDefault", True, Boolean)
        frequencyIsDefault.AddAttribute(BrowsableAttribute, False)
        frequencyIsDefault.AddAttribute(XmlIgnoreAttribute) # This value does not need to be saved in the test plan file.

        # It is also possible to add methods.
        resetFrequency = self.RegisterMethod("resetFrequency", None);
        resetFrequency.AddAttribute(BrowsableAttribute, True) # Making a method browsable makes it available in the user interface as a button.
        resetFrequency.AddAttribute(OpenTap.EnabledIfAttribute, "FrequencyIsDefault", False, HideIfDisabled=True)
        resetFrequency.AddAttribute(OpenTap.DisplayAttribute, "Reset Frequency", None)

        selectable = self.AddProperty("Selectable", 0, Int32)
        selectable.AddAttribute(OpenTap.AvailableValuesAttribute, "Available")
        selectable.AddAttribute(OpenTap.DisplayAttribute, "Selectable", "Values from Available Values can be selected here.", "Selectable")
        available = self.AddProperty("Available", [1,2,3], List[Int32])
        available.AddAttribute(OpenTap.DisplayAttribute, "Available Values", "Select which values are available for 'Selectable'.", "Selectable")
        self.Available.append(4) # the backing data behaves as a python list in this case.

        # C# type 'Enabled<T>' allows the users to specify the status (enable/disable) and the value of the property.
        logging = self.AddProperty("Logging", OpenTap.Enabled[String](), OpenTap.Enabled[String])
        logging.AddAttribute(OpenTap.DisplayAttribute, "Logging", "Path of where the log file will be stored.", "Result Logging", 1)
        self.Logging.IsEnabled = True
        self.Logging.Value = "C:\\SessionLogs\\"

    # __setattr__ demonstrates the use of overriding __setattr__ to raise a notification
    # the a given property has changed.
    def __setattr__(self, name, value):
        super(BasicFunctionality, self).__setattr__(name, value)
        if name == "Frequency":
            self.OnPropertyChanged("Frequency")
            self.FrequencyIsDefault = abs(self.Frequency - 1e9) < 0.001

    def resetFrequency(self):
        self.Frequency = 1e9

    def Run(self):
        # Write some log messages
        self.Debug("Frequency: {0}Hz", self.Frequency)
        self.Info("Info message")
        self.Error("Error message")
        self.Warning("Warning Message")
        
        # Create some results
        for i in range(0, 200):
            self.PublishResult("MyResults1", ["X", "Y"], [i,math.sin(i * 0.1 * 0.5) + math.sin(i * 0.15 * 0.5)]);
            self.PublishResult("MyResults2", ["X", "Y"], [i,math.sin(i * 0.3 * 0.5) + math.sin(i * 0.2 * 0.5)]);

        # Run the child steps
        for step in self.EnabledChildSteps:
            self.RunChildStep(step)
        
        # call method on the instrument.
        print("Measurement :" + str(self.Instrument.do_measurement()) + " dBm")
        
        self.Dut.init_high_power_mode()
        print("High power mode? " + str(self.Dut.HighPowerOn))

        # Using component settings defined in python:
        print("Number of points for fft: " + str(BasicSettings.GetCurrent().NumberOfPoints))
        
        # Set verdict
        self.UpgradeVerdict(OpenTap.Verdict.Pass)
        self.Frequency = self.Frequency + 10;