# Getting Started
The best way to get started is to explore the example plugins. 
These can be downloaded from [packages.opentap.io](http://packages.opentap.io/index.html#/?name=PythonExamples).

Using any supported OpenTap installation you can run:

```tap package install PythonExamples```

The package contains a number of Python files that shows how to use many of the OpenTAP features that you will need to develop you own plugins.
The files are located inside the Packages/PythonExamples inside your installation folder.

The process to get up and running depends a bit on which platform you are on, please follow one of the following:

### Windows and MacOS

We recommend using the installers from https://www.python.org. 

### Ubuntu

On Ubuntu we recommend installing Python using apt. 

e.g 
```apt install python3.10```

### Other Platforms and custom Python installations

On other platforms we might not be able to detect the python installation. 
In this case you can set the python library and installation path manually.
Here is an example of how this is done on a Mac OS GitHub builds:
```
          ./bin/Debug/tap python set-path $Python3_ROOT_DIR
          ./bin/Debug/tap python set-lib-path $Python3_ROOT_DIR/lib/libpython3.10.dylib
```

# Create and Run a Simple Test Plan
To just run the 'Basic Functionality' step, you can create a test plan with the following components in the GUI:

1. Add **Python Example / Basic Functionality** from the New Step dialog, located in the **Test Plan** panel.

2. In the DUT settings, add **Python Example / Basic DUT** (PyDUT).

3. In the Instrument settings, Add the **Python Example / Basic Instrument** (PyInstrument).

4. In the **Step Settings** panel, ensure that the DUT and instrument resources are assigned to the step.

5. In the **Test Plan** panel, click the **Run** button (F5).

The **Log** panel shows that the test passed:

![](Images/python_passed.png)

Now we recommend exploring the rest of the Python Examples project. Try for example:
- Running a battery charge / discharge scenario with the simulated power analyzer.
- Connect steps using the Input/Output examples
- Try modifying the Basic Functionality example to include your own log messages. You can also try this with code reloading enabled (In Python settings).