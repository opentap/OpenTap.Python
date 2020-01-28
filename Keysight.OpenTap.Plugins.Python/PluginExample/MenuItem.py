"""
This example shows how to implement an IMenuItem plugin interface that interacts with Editor graphical user interface.

Since the OpenTAP interfaces are .NET interfaces they have to be implemented by registering the methods and properties that are required by that interface.
The IMenuItem interface has the following specification:
    
    public interface IMenuItem : ITapPlugin
    {
        void Invoke();
    }

The interface requires one method to be implemented, Invoke, invoke takes no arguments so implementing it is quite simple. It also requires that a DisplayAttribute is used to show the name and group of the menu item.

When used correctly, the menu item can be seen in the Editor menubar.
    
"""
import OpenTap
import PythonTap
from PythonTap import *

import System
clr.AddReference("Keysight.OpenTap.Wpf")
import Keysight.OpenTap.Wpf
from Keysight.OpenTap.Wpf import IMenuItem

@PluginType # Declare that this is a plugin type that should be wrapped to C#
@AddInterface(IMenuItem) # Declare that we intend to implement IMenuItem
# Put 'Python Menu Item' on the button and place it in the Python menu.
@Attribute(OpenTap.DisplayAttribute, "Print Message", Group="Python Example")
class MenuItem(TapPlugin(GenericPythonObject)):
    
    def __init__(self):
        super(MenuItem, self).__init__()

        # Register the interface methods and the corresponding arguments.
        self.RegisterMethod("Invoke", None)

    # Define what happens when the menu item is clicked. 
    # Note, this blocks the graphical user interface thread, so long running calls should no be
    # made here.
    def Invoke(self):
        print("Menu item clicked!")
