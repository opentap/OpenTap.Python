"""
 This is an example of using inputs and outputs to share data between test steps.
"""
from opentap import *
import OpenTap
from OpenTap import Display, Output, Input
from System import Double
# OutputStep is pairing with InputStep. 
# As an example, the generic output of the parameter OutputValue[double] from the step can be shown in step 'InputStep'
@attribute(Display("Test Step Output", "An example of using outputs to share data with other test steps.", "Python Example"))
class OutputStep(TestStep):
    OutputValue = property(Double, 0.0)\
        .add_attribute(Display("Output Value", "", "Output", 0))\
        .add_attribute(Output())
    
    def Run(self):
        # Create some result to manipulate value of Output value
        self.OutputValue = self.OutputValue + 10

# OutputStep is pairing with InputStep. 
# As an example, the property 'InputValue' can be setup to output the value from property 'OutputValue' from 'OutputStep'
@attribute(Display("Test Step Input", "An example of using data shared from an output step as an input.", "Python Example"))
class InputStep(TestStep):

    # This property shows how generic Input[double] is used. 
    # As an example, in Output step, the property "Output Value" (double) is assigned "OutputAttribute". 
    # The value of that property will be captured as output and used as input here.
    InputValue = property(Input[Double], None)\
        .add_attribute(Display("Input Value"))
    def __init__(self):
        super().__init__() # The base class initializer should be invoked.
        self.InputValue = Input[Double]()
        
    def Run(self):
        # Create some result to manipulate value of Output value
        # Now we can use "Output Value" property from OutputStep to display "Input value" property. 
        # These are the condition to fulfill before displaying the value:
        # 1. Both steps (Output Test Step and Intput Test Step) have to present in the test plan.
        # 2. The property "Input value" in this step has to be setup to "Output value from Output Test Step".
        try:
            self.log.Info("Input value: {0}", self.InputValue.Value)
        except Exception as e:
            self.log.Error("Input value is not configured to the Output value.")
            self.log.Debug(e)