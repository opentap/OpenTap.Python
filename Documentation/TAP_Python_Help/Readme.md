# Welcome
Python is a popular programming language for test and automation. The OpenTAP Python plugin makes it possible to use Python to program plugins for OpenTAP.

With the Python plugin you can:

- Access the OpenTAP API when creating plugins in Python. TestSteps, Instruments, DUTs, ResultListeners, and ComponentSettings can be developed in Python. 
- Use your preferred programming environment, such as Python Tools for Visual Studio (PTVS) or PyCharm.
- Leverage existing Python code. 
- Integrate with other OpenTAP plugins. 

The following example shows how a test step plugin can be defined in Python:

```py
class MyPythonStep(TestStep):
   def __init__(self):
      super(MyPythonStep, self).__init__()

   def Run(self):
      self.log.Debug("Hello from Python")
```

All other normal OpenTAP SDK constructs can be used in a similar fashion:

- Core components of the OpenTAP C# API are directly supported. 
- .NET types and OpenTAP classes can be used. 
- Attributes let you create user friendly configurations for your plugins.

Refer to the OpenTAP SDK for OpenTAP constructs that can be created.

To get started, see [Getting Started](./Python_Development_Examples/Readme.md)
