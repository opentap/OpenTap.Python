"""
Plugins can be hidden from the user by using the browsable attribute. 
When used with settings this can be used to make settings that can be saved and loaded, 
but not viewed by the user except from by going directly to the XML file.
"""
from opentap import *
import OpenTap
from System.ComponentModel import BrowsableAttribute
# This DUT will not show up in the list of available DUTs.
@attribute(BrowsableAttribute, False)
class HiddenDut(Dut):
    def __init__(self):
        super(HiddenDut, self).__init__()
        pass