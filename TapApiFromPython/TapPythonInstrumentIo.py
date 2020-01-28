#region Imports
import clr
import os
import sys

tapPath = os.environ["TAP_PATH"]
sys.path.append(tapPath)
print clr.__file__
clr.AddReference("OpenTap")
clr.AddReference("System")
clr.AddReference("Keysight.OpenTap.Plugins.Python.PyHelper")

from System.Diagnostics import *
from System.Collections.Generic import List
from OpenTap import *
from OpenTap.Plugins.Python import *
#endregion

#TAP Logging event handler
def EventsLogged(events):
    for event in events:
        #Filter out verbose messages
        #if event.EventType != TraceEventType.Verbose:
        print event.Message
    return

class InitLogListener():
    logListener = None

    def __init__(self):
        self.logListener = PyHelper.LogListener()
        self.logListener.PyEventsLogged += EventsLogged

testPlanFilePath = None
logListener = InitLogListener()

def main(argv):

    try:
        for instrument in InstrumentSettings.Current:
            print instrument.Name

        #This example communicates with VSA89601B 
        #configured as a SCPI instrument on localhost
        pyi = PyHelper.ScpiInstrument()

        pyi.VisaAddress = "TCPIP0::127.0.0.1::hislip1::INSTR"
        pyi.Open()
        retval = pyi.ScpiQuery("*IDN?")
        pyi.Close()

        print retval

    except Exception as inst:
        print "Caught Exception: "
        print type(inst)     # the exception instance
        print inst.args      # arguments stored in .args
        print inst           # __str__ allows args to be printed directly
    
    #Wait for user input before exiting cmd window
    raw_input("")
    sys.exit(0)
    return

if __name__ == "__main__":
    main(sys.argv)
