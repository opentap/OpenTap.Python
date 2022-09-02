"""
 This file contains infrastructure for getting OpenTAP to work with Python.
"""
__copyright__ = """
  Copyright 2012-2022 Keysight Technologies
  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at
  http://www.apache.org/licenses/LICENSE-2.0
"""

import sys
import clr
import math
import types
import traceback
import weakref
import subprocess
import os

clr.AddReference("OpenTap")
clr.AddReference("OpenTap.Python")
import OpenTap
import OpenTap.Python
from System.ComponentModel import Browsable, BrowsableAttribute
import System
from System import String, Double, Array, IConvertible
from System.Collections.Generic import List

# find the installed python executable.
# the sys.executable contains dotnet, so it is not useful
# for starting a python sub process. 
def find_python():
    for name in ["python3", "py.exe", "python.exe"]:
        p = os.path.join(sys.prefix, "bin", name)
        if os.path.isfile(p):
            return p
        p = os.path.join(sys.prefix, name)
        if os.path.isfile(p):
            return p
    return None

pyexe = find_python()      

if pyexe != None:
    # sys.executable is set to dotnet. This is not really useful for anybody
    # hence we set it to the python executable so that scripts can be run.
    sys.executable = pyexe

def install_package(file):
    subprocess.check_call([pyexe, '-m', 'pip', 'install', '-r', os.path.abspath(file)])

debugpy_imported = False

try:
    # setup debugging this is done using debugpy, but is an optional feature.
    if OpenTap.Python.PythonSettings.Current.Debug:
        import debugpy
        debugpy.configure(subProcess = False)
        debugpy.listen(OpenTap.Python.PythonSettings.Current.DebugPort)
        debugpy_imported = True
except Exception as e:
    print("Could not enable debugging: " + str(e))
    
attribute = clr.attribute

def debug_this_thread():
    if debugpy_imported:
        debugpy.debug_this_thread()
    else:
        pass

class Rule(OpenTap.Python.VirtualValidationRule):
    def __init__(self, property, validFunc, errorFunc):
        super(Rule, self).__init__(property)
        self.validFunc = validFunc
        self.errorFunc = errorFunc
    def Error():
        if self.validFunc():
            return None
        return self.errorfunc()

property = clr.property
method = clr.clrmethod

class Trace(OpenTap.TraceSource):
    def __init__(self, resource):
        self.resource = resource
    def Debug(self, message, *args):
        """ Log a debug level message. Can also be used to log the stacktrace of an exception. """
        if isinstance(message, BaseException):
             traceback.print_exc(file = Logger(self.resource.Log))
        else:
            if isinstance(message, str):
                OpenTap.Log.Debug(self.resource.Log, message, args)
            else: 
                OpenTap.Log.Debug(self.resource.Log, message, args[0], args[1:])
                
    def Info(self, message, *args):
        """ Log a debug level message. Can also be used to log the stacktrace of an exception. """
        if isinstance(message, BaseException):
             traceback.print_exc(file = Logger(self.resource.Log))
        else:
            if isinstance(message, str):
                OpenTap.Log.Info(self.resource.Log, message, args)
            else: 
                OpenTap.Log.Info(self.resource.Log, message, args[0], args[1:])
    def Warning(self, message, *args):
        """ Log a debug level message. Can also be used to log the stacktrace of an exception. """
        if isinstance(message, BaseException):
             traceback.print_exc(file = Logger(self.resource.Log))
        else:
            if isinstance(message, str):
                OpenTap.Log.Warning(self.resource.Log, message, args)
            else: 
                OpenTap.Log.Warning(self.resource.Log, message, args[0], args[1:])
    def Error(self, message, *args):
        """ Log a debug level message. Can also be used to log the stacktrace of an exception. """
        if isinstance(message, BaseException):
             traceback.print_exc(file = Logger(self.resource.Log))
        else:
            if isinstance(message, str):
                OpenTap.Log.Error(self.resource.Log, message, args)
            else: 
                OpenTap.Log.Error(self.resource.Log, message, args[0], args[1:])
class Logger:
    """Internal: Replaces the print statements to use the Keysight Test Automation logger."""
    def __init__(self, log = None, level = OpenTap.LogEventType.Debug):
        self.terminal = sys.stdout
        if log == None:
            log = OpenTap.Log.CreateSource("Python")
        self.log = log
        self.level = level

    def write(self, message):
        if len(message) == 1 and message[0] == '\n':
            return
        self.log.TraceEvent(self.level, 0, message)

    def flush(self):
        self.log.Flush()
        self.terminal.flush()

sys.stdout = Logger()
sys.stderr = Logger(OpenTap.LogEventType.Error)

def reload_module(module):
    """Internal: Reloads modules and sub-modules. Similar to imp.reload, but recurses to included sub-modules."""
    import imp
    import types
    things_to_reload = [module]
    loaded = {}
    basename = module.__name__
    toload = []
    while len(things_to_reload) > 0:
        modname = things_to_reload.pop(0)
    
        try:
            if modname.__name__ in loaded:
                    continue
            toload.append(modname)
            loaded[modname.__name__] = True
            
        except:
            continue
        
        for key,value in modname.__dict__.items():
            
            if str(value).startswith('<class \'' + basename):
                if value.__module__ in loaded:
                    continue
                value = sys.modules[value.__module__]
            # sub-module names starts with basename.submodulename
            if isinstance(value, types.ModuleType) and value.__name__.startswith(basename):
                
                things_to_reload.append(value)
    toload2 = []
    # delete the modules from sys.modules before loading again.
    for x in toload:
        toload2.append(x.__name__)
        del sys.modules[x.__name__]

    for x in reversed(toload2):
        print("Reloading :" + str(x))
        LoadModule(x)

def LoadModule(modname):
    return __import__(modname)

# common base classes
class PyTestStep(OpenTap.TestStep):
    __namespace__ = "OpenTap.py"
    __clr_abstract__ = True
    def __init__(self):
        super().__init__()
        self.log = Trace(self)
    def PrePlanRun(self):
        debug_this_thread()
    def Run(self):
        debug_this_thread()
        
    def PublishResult(self, tableName, columnNames, rows):
        if len(rows) == 0:
            return
        names = List[String]()
        r = Array[IConvertible](len(rows))
        for i in range(len(rows)):
            r[i] = Double(rows[i])
        for name in columnNames:
            names.Add(name)
        if isinstance(rows[0], list):
            pass
        self.Results.Publish(tableName, names, r)

class PyDut(OpenTap.Dut):
    __clr_abstract__ = True
    __namespace__ = "OpenTap.py"
    def __init__(self):
        super().__init__()
        self.log = Trace(self)
    def Open(self):
        super().Open()
    def Close(self):
        super().Open()


class PyInstrument(OpenTap.Instrument):
    __clr_abstract__ = True
    __namespace__ = "OpenTap.py"

    def __init__(self):
        super().__init__()
        self.log = Trace(self)
    def Open(self):
        super().Open()
    def Close(self):
        super().Open()

class PyResultListener(OpenTap.ResultListener):
    __clr_abstract__ = True
    __namespace__ = "OpenTap.py"
    
    def __init__(self):
        super().__init__()
        self.log = Trace(self)
        
    def Open(self):
        super().Open()
    def Close(self):
        super().Close()

ResultListener = PyResultListener
TestStep = PyTestStep
Instrument = PyInstrument
Dut = PyDut
