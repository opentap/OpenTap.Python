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
loadLockManager = False
if loadLockManager:
    import OpenTap
    import opentap
    from opentap import *
    
    import System
    from System.Collections.Generic import IEnumerable
    from System.Threading import CancellationToken
    
    class LockManager(OpenTap.ILockManager):
        
        def __init__(self):
            super(LockManager, self).__init__()
    
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
