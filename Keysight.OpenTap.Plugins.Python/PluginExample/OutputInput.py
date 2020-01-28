"""
 This is an example of using inputs and outputs to share data between test steps.
"""
from PythonTap import *
import OpenTap
from System import Double
# OutputStep is pairing with InputStep. 
# As an example, the generic output of the parameter OutputValue[double] from the step can be shown in step 'InputStep'
@Attribute(OpenTap.DisplayAttribute, "Test Step Output", "An example of using outputs to share data with other test steps.", "Python Example")
class OutputStep(TestStep):
    def __init__(self):
        super(OutputStep, self).__init__()

        # Add Output property
        prop = self.AddProperty("OutputValue", 0.0, Double)
        prop.AddAttribute(DisplayAttribute, "Output Value", "", "Output", 0)
        prop.AddAttribute(OutputAttribute)
    
    def Run(self):
        # Create some result to manipulate value of Output value
        self.OutputValue = self.OutputValue + 10

# OutputStep is pairing with InputStep. 
# As an example, the property 'InputValue' can be setup to output the value from property 'OutputValue' from 'OutputStep'
@Attribute(OpenTap.DisplayAttribute, "Test Step Input", "An example of using data shared from an output step as an input.", "Python Example")
class InputStep(TestStep):
    def __init__(self):
        super(InputStep, self).__init__() # The base class initializer must be invoked.

        # This property shows how generic Input[double] is used. 
        # As an example, in Output step, the property "Output Value" (double) is assigned "OutputAttribute". 
        # The value of that property will be captured as output and used as input here.
        prop = self.AddProperty("InputValue", Input[Double](), Input[Double])
        prop.AddAttribute(DisplayAttribute, "Input Value")
    
    def Run(self):
        # Create some result to manipulate value of Output value
        # Now we can use "Output Value" property from OutputStep to display "Input value" property. 
        # These are the condition to fulfill before displaying the value:
        # 1. Both steps (Output Test Step and Intput Test Step) have to present in the test plan.
        # 2. The property "Input value" in this step has to be setup to "Output value from Output Test Step".
        try:
            self.Info("Input value: {0}", self.InputValue.Value)
        except Exception as e:
            self.Error("Input value is not configured to the Output value.")
            self.Debug(e)