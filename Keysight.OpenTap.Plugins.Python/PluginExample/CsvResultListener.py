"""
Example of how to make a Result Listener.
"""
import sys
import clr
import math
import PythonTap
from PythonTap import *


from System.IO import *
from System.Text import StringBuilder
from System import String
import OpenTap
from OpenTap import Log, DisplayAttribute, FilePathAttribute

@Attribute(DisplayAttribute, "CSV ResultListener", "An example of a ResultListener.", "Python Example")
class CsvPythonResultListener(ResultListener):
    def __init__(self):
        super(CsvPythonResultListener, self).__init__() # The base class initializer must be invoked.
        prop = self.AddProperty("FilePath", "MyFile.csv", String);
        prop.AddAttribute(FilePathAttribute, FilePathAttribute.BehaviorChoice.Open, ".csv")
        prop.AddAttribute(DisplayAttribute, "File Path", "File path for results.")
        self.sb = StringBuilder()
        self.Name = "PyCSV"

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

    def OnTestStepRunCompleted(self, stepRun):
        """Called by TAP when a test step completes."""
        pass 

    def OnTestPlanRunCompleted(self, planRun, logStream):
        """Called by TAP when the test plan completes."""
        self.Info("Python ResultListener TestPlanRunCompleted")
        try:
            File.WriteAllText(self.FilePath, self.sb.ToString())
        except Exception as e:
            self.Debug(e)

    def Open(self):
        """Called by TAP when the test plan starts."""
        self.Info("Python CSV ResultListener Opened")

    def Close(self):
        """Called by TAP when the test plan ends."""
        self.Info("Python CSV ResultListener Closed")
