"""
An example of a step that throws and handles an error.
"""
from PythonTap import *
import OpenTap

@Attribute(OpenTap.DisplayAttribute, "Error Handling", "An example of a step that throws and handles an error.", "Python Example")
class ErrorExample(TestStep):
    def __init__(self):
        super(ErrorExample, self).__init__()

    def Run(self):
        try:
            x = 1 / 0
        except Exception as e:
            self.Error("Caught error dividing by zero.")
            self.Debug(e)