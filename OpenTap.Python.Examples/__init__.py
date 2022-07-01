# Imported the plugin modules.
# These modules should be located relative to this __init__ file.
# It denotes that the module is in the same folder.
import sys
#from .EnumUsage import *
from .BasicFunctionality import *
from .BasicSettings import *
from .BasicDut import *
from .BasicInstrument import *
from .ErrorExample import *
from .HiddenDut import *
from .OutputInput import *
from .CsvResultListener import *
from .PowerAnalyzer import *    
from .SamplingStepBase import *
from .ChargeStep import *
from .DischargeStep import *
from .CliAction import *


## Advanced Section ##
IncludeLockManager = False 
#IncludeLockManager = True # Uncomment this to include the lock manager example.
if IncludeLockManager:
    from .LockManager import *

#This only works on with Editor (Community or Enterprise):
IncludeMenuItem = False
#IncludeMenuItem = True # Uncomment this to include the menu item example.
if IncludeMenuItem:
    from .MenuItem import *