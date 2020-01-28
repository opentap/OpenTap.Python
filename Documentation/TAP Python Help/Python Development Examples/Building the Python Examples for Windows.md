# Building the Python Examples for Windows
Follow these steps to build the examples:

1. In the Editor application, ensure that you have configured using the right Python implementation in the Python settings. To do so, configure the `Python Path` setting. This can also be done from the command line by executing `tap python set-path <Your python path, e.g C:\Python27\>`.

2. Open the Command Prompt and navigate to your Python plugin folder (%TAP_PATH%).

3. Build the wrapper for the example modules. 
Execute the command `tap python build PluginExample`
```cmd
C:\Program Files\Keysight\Test Automation>tap python build PluginExample
Start building PluginExample.
Building EnumUsage plugin.
Building BasicInstrument plugin.
Building BasicDut plugin.
Building BasicSettings plugin.
Building BasicFunctionality plugin.
Building ErrorExample plugin.
Building HiddenDut plugin.
Building OutputStep plugin.
Building InputStep plugin.
Building CsvPythonResultListener plugin.
Building PowerAnalyzer plugin.
Building SamplingStepBase plugin.
Building ChargeStep plugin.
Building DischargeStep plugin.
Building Color1 plugin.
Building Color2 plugin.
Building Color3 plugin.
Building InputImpedanceEnum plugin.
PluginExample Completed.
```

4. Start the OpenTAP and look at the example plugin(s) in the steps panel. The example plugin contains a basic example of each Keysight Test Automation SDK component:

 - Five **Test Steps** available through the Add New Step dialog:
    
    - Basic Functionality
	- Battery Charge
    - Battery Discharge
    - Enum Usage (Python 3.x only)
	- Error Handling
	- Test Step Input
	- Test Step Output

 - One **Device Under Test** available through the Settings > Bench > DUTs menu:
    - Basic DUT

 - Two **Instruments** available through the Settings > Bench > Instruments menu:
    - Basic Instrument
    - Power Analyzer

 - One **Result Listener** available through the Settings > Result menu: 
    - CSV Result Listener

 - One **Component Settings** accessible in the Settings dialog.
    - Basic Settings
 

You can [Create and Run a Simple Test Plan](Create and Run a Simple Test Plan for Windows.md) using these steps and resources to become more familiar with OpenTAP.
