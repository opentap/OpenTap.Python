import Tap
import clr
clr.AddReference("OpenTap.dll")
clr.AddReference("Keysight.OpenTap.Plugins.Python.dll")
import OpenTap
print OpenTap.TestStep

class PythonStep2(OpenTap.TestStep):
    pass

x = PythonStep2()
print x

#@Tap.AllowAnyChild
class PythonStep(Tap.TestStep):
    def __init__(self):
        print "Creating step"
        #@unit("Hz") # This does not work.
        self.Frequency = 1.9e9
        self.SampleCount = 5
        #@display("Instrument", "The instrument selected by the user")
        self.Instrument = 0

    #This is one way of creating a property.
    @Tap.Unit("Hz")
    def GetFrequency(self):
        return self.Frequency;
    def SetFrequency(self, value):
        self.Frequency = value;

    @Tap.Unit(".")
    @Tap.Display("Sample Count")    
    def GetSampleCount(self):
        return self.SampleCount
    def SetSampleCount(self, value):
        self.SampleCount = value
    
    def Run(self):
        result = self.Instrument.DoMeasurement(self.Frequency);
        self.RunChildSteps()
        Result.Publish(result)

class PythonInstrument(Tap.Instrument):
    def __init__(self):
        self.VisaAddress = "TCPIP0::127.0.0.1::instr"
        self.PathLoss = 0.0;

    def Open(self):
        base.Open(self)
        log.Debug("The instrument is now open!")
        
    def Close(self):
        base.Close(self)
        log.Debug("The instrument is now closed!")

class PythonDut(Tap.Dut):
    def __init__(self):
        pass
    def Open(self):
        base.Open(self)
        log.Debug("DUT is open")
    def Close(self):
        base.Open(self)
        log.Debug("DUT is closed");



mystep = PythonStep()
print mystep
print mystep.GetFrequency()
print Tap.get_unit(mystep.__class__.__dict__['GetFrequency'])
print mystep.__class__.__dict__
