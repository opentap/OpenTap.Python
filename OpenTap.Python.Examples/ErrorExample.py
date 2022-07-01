"""
An example of a step that throws and handles an error.
"""
from opentap import *
import OpenTap

@attribute(OpenTap.DisplayAttribute, "Error Handling", "An example of a step that throws and handles an error.", "Python Example")
class ErrorExample(TestStep):
    def __init__(self):
        super().__init__()

    def Run(self):
        try:
            x = 1 / 0
        except Exception as e:
            self.log.Error("Caught error dividing by zero.")
            self.log.Debug(e)