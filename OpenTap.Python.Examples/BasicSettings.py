"""
A basic example of how to define settings.
This will show up in settings and cause a XML file to be generated in the Settings folder.
"""
from opentap import *
import OpenTap
from System import Int32

@attribute(OpenTap.Display("Example Settings", "A basic example of how to define settings."))
class BasicSettings(OpenTap.ComponentSettings):
    def __init__(self):
        super().__init__()
    NumberOfPoints = property(Int32, 600)\
        .add_attribute(OpenTap.Display("Number of points"))