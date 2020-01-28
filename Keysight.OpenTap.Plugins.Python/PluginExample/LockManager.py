"""
This example shows how to implement a OpenTAP plugin interface. Concretely, it shows how a ILockManager plugin can be implemented.

Since the OpenTAP intefaces are .NET interfaces they have to be implemented by registering the methods and properties that are required by that interface.
The ILockManager interface has the following specification:

    public interface ILockManager : ITapPlugin
    {
        void AfterClose(IEnumerable<IResourceReferences> resources, CancellationToken abortToken);
        void BeforeOpen(IEnumerable<IResourceReferences> resources, CancellationToken abortToken);
    }

The interface requires two methods to be implemented, AfterClose and BeforeOpen, with the respective arguments and argument types. In the example below it is shown how its implemented.

For more information about the ILockManager interface, please refer to the OpenTAP API Reference.

"""
import OpenTap
import PythonTap
from PythonTap import *

import System
from System.Collections.Generic import IEnumerable
from System.Threading import CancellationToken

@PluginType # Declare that this is a plugin type that should be wrapped to C#
@AddInterface(OpenTap.ILockManager) # Declare that we intend to implement ILockManager
class LockManager(TapPlugin(GenericPythonObject)):
    
    def __init__(self):
        super(LockManager, self).__init__()

        # Register the interface methods and the corresponding arguments.
        beforeOpen = self.RegisterMethod("BeforeOpen", None)
        beforeOpen.AddArgument("resources", IEnumerable[OpenTap.IResourceReferences])
        beforeOpen.AddArgument("abortToken", CancellationToken)

        afterClose = self.RegisterMethod("AfterClose", None)
        afterClose.AddArgument("resources", IEnumerable[OpenTap.IResourceReferences])
        afterClose.AddArgument("abortToken", CancellationToken)

    #Define what happens before open
    def BeforeOpen(self, resources, abortToken):
        #Logic that locks the resource can be implemented here.
        for resourceNode in resources:
            print("Locking: " + str(resourceNode.Resource))

    #Define what happens after close.
    def AfterClose(self, resources, abortToken):
        #Logic that unlocks the resource can be implemented here.
        for resourceNode in resources:
            print("Unlocking: " + str(resourceNode.Resource))
