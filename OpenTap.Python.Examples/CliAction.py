"""
 A basic example of how to define a Cli Action.
"""
from System import String
import System.Threading
import OpenTap
import OpenTap.Cli
from opentap import *


@attribute(OpenTap.Display("test-cli", "A basic example of an OpenTap Cli action. write ./tap pythonexample test-cli", "pythonexample"))
class CliAction2(OpenTap.Cli.ICliAction):
    
    IpAddress = property(String, "127.0.0.1")\
        .add_attribute(OpenTap.Display("IP Address", "The IP Address of the instrument."))\
        .add_attribute(OpenTap.Cli.CommandLineArgument("ip"))
         
    @method(int,[System.Threading.CancellationToken])
    def Execute(self, cancellationToken):
        """Called by TAP when the test plan ends."""
        print(self.IpAddress)
        return 0