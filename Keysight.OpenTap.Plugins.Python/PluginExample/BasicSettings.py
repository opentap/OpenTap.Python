"""
A basic example of how to define settings.
This will show up in settings and cause a XML file to be generated in the Settings folder.
"""
from PythonTap import *
import OpenTap
from System import Int32

@Attribute(OpenTap.DisplayAttribute, "Example Settings", "A basic example of how to define settings.")
class BasicSettings(ComponentSettings):
    def __init__(self):
        super(BasicSettings, self).__init__() # The base class initializer must be invoked.
        prop = self.AddProperty("NumberOfPoints", 600, Int32);
        prop.AddAttribute(OpenTap.DisplayAttribute, "Number of points")