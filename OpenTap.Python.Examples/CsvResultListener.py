"""
Example of how to make a Result Listener.
"""
import sys
import clr
import math
import opentap
from opentap import *
import os

import System.IO
from System.Text import StringBuilder
from System import String
import OpenTap
from OpenTap import Log, DisplayAttribute, Display, FilePathAttribute, FilePath

@attribute(Display("CSV ResultListener", "An example of a ResultListener.", "Python Example"))
class CsvPythonResultListener(PyResultListener):
    # Use MacroString to get access to various macros for the file name.
    # The <ResultName> macro is not supported in this example code as all the results 
    # are written to the same file.
    FilePath = property(OpenTap.MacroString, None)\
        .add_attribute(FilePath(FilePathAttribute.BehaviorChoice.Open, ".csv"))\
        .add_attribute(Display("File Path", "File path for results."))
    def __init__(self):
        super(CsvPythonResultListener, self).__init__() # The base class initializer must be invoked.
        self.sb = StringBuilder()
        self.Name = "PyCSV"
        self.FilePath = OpenTap.MacroString()
        self.FilePath.Text = "<Date>/test.csv"

    def OnTestPlanRunStart(self, planRun):
        """Called by TAP when the test plan starts."""
        pass

    def OnTestStepRunStart(self, stepRun):
        """Called by TAP when a test step starts."""
        pass

    def OnResultPublished(self, stepRun, result):
        """Called by TAP when a chunk of results are published."""
        self.OnActivity()
        for row in range(0, result.Rows):
            first = True
            for col in range(0, result.Columns.Length):
                if first:
                    first = False
                else:
                    self.sb.Append(", ")
                self.sb.Append(str(result.Columns[col].Data.GetValue(row)))
            self.sb.AppendLine("")
    def OnTestPlanRunCompleted(self, planRun, logStream):
        """Called by TAP when the test plan completes."""
        try:
            # this code expands the macros used by FilePath.
            fileName = self.FilePath.Expand(planRun)
            
            # Create directory if not exists
            directory = os.path.dirname(fileName)
            if not os.path.exists(directory):
                os.makedirs(directory)

            # Then write to the file.
            System.IO.File.WriteAllText(fileName, self.sb.ToString())
        except Exception as e:
            self.log.Debug(e)
    
    def OnTestStepRunCompleted(self, stepRun):
        """Called by TAP when a test step completes."""
        pass
        
    def Open(self):
        """Called by TAP when the test plan starts."""
        super(CsvPythonResultListener, self).Open()
        
    def Close(self):
        """Called by TAP when the test plan ends."""
        super(CsvPythonResultListener, self).Close()
        