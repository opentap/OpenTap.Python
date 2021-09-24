"""
 This file contains infrastructure for getting OpenTAP to work with Python.
"""
__copyright__ = """
  Copyright 2012-2019 Keysight Technologies
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
clr.AddReference("OpenTap");
clr.AddReference("Keysight.OpenTap.Plugins.Python")
import OpenTap
import OpenTap.Plugins.BasicSteps

import Keysight.OpenTap.Plugins.Python
import System

from OpenTap import *
from Keysight.OpenTap.Plugins.Python import *
from Keysight.Plugins.Python import *

isstr = None

if sys.version_info[0] == 3:
    import enum
    from enum import Enum
    def isstring(x):
        return isinstance(x, str)
    isstr = isstring
else:
    def isstring(x):
        return isinstance(x, basestring)
    isstr = isstring
    

plugin_types = set()
def add_plugin_type(type):
    plugin_types.add(type)

def _get_tap_classes(items):
    for item in items:
        for type in plugin_types:
            if issubclass(item, type):
                yield item;
                break
def get_tap_classes(items):
    return list(_get_tap_classes(items))

plugins = {}

def Plugin(type):
    if type.__name__ in plugins:
        pass
    else:
        plugins[type.__name__] = type
    return type

def GetPlugins():
    return list(plugins)

def GetPlugin(name):
    if name in plugins:
        return plugins[name]
    else:
        None

def PluginType(type):
    add_plugin_type(type)
    return type

class_interfaces = {}
def _add_interface(cls, interface):
    if not(cls in class_interfaces):
        class_interfaces[cls] = set()
    class_interfaces[cls].add(interface)
def AddInterface(interface):
    def attach_interface(cls):
        _add_interface(cls, interface)
        return cls
    return attach_interface
    

def TapPlugin(base):
    class _TapPlugin(base):
        
        def __init__(self):
            self.pythonObject = self
            self.__ruleCollection = {}
            
        def reload(self):
            self.__class__ = sys.modules[type(self).__module__].__dict__[type(self).__name__] 

        def AddProperty(self, name, value, type):
            setattr(self, name, value)
            return _addClassProperty(self.__class__, name, value, type)
        
        def RegisterMethod(self, methodname, returnType):
            return _addClassMethod(self.__class__,methodname, returnType)
    
        def Debug(self, message, *args):
            """ Log a debug level message. Can also be used to log the stacktrace of an exception. """
            if isinstance(message, BaseException):
                traceback.print_exc(file = Logger(self.Log))
            else:
                if isstr(message):
                    OpenTap.Log.Debug(self.Log, message, args)
                else: 
                    OpenTap.Log.Debug(self.Log, message, args[0], args[1:])
    
        def Info(self, message, *args):
            """ Log an info level message. """
            if isstr(message):
                OpenTap.Log.Info(self.Log, message, args)
            else: 
                OpenTap.Log.Info(self.Log, message, args[0], args[1:])
        
        def Warning(self, message, *args):
            """ Log a warning level message. """
            OpenTap.Log.Warning(self.Log, message, args)
        
        def Error(self, message, *args):
            """ Log an error level message. """
            OpenTap.Log.Error(self.Log, message, args)

        def AddRule(self, rule, errorMessage, propertyName):
            """ Add rule to the step. """
            key = '{}-{}'.format(propertyName, hash(rule))
            self.__ruleCollection[key] = {'Rule': rule, 'ErrorMessage': errorMessage, 'PropertyName': propertyName}
        
        def getErrorMessage(self, input):
            if isinstance(input, types.FunctionType):
                return input().strip()
            elif isinstance(input, str):
                return input.strip()

        def getError(self):
            errors = []
            def pushError(error, errors):
                if error not in errors:
                    errors.append(error)

            def checkDisplayCondition(cls, propertyName):
                if cls not in class_properties:
                    return False
                properties = class_properties[cls]
                if propertyName not in properties:
                    return False
                property = properties[propertyName]
                if OpenTap.EnabledIfAttribute not in property.Attributes:
                    return True
                attr = property.Attributes[OpenTap.EnabledIfAttribute]
                if attr[2]['HideIfDisabled'] is False:
                    return True
                result = False
                if hasattr(self, attr[1][0]):
                    i = 1
                    while i < len(attr[1]):
                        if getattr(self, attr[1][0]) is attr[1][i]:
                            result = True
                        i += 1
                return result

            for rule in self.__ruleCollection.values():
                try:
                    if rule['Rule']() == True:
                        continue
                    if checkDisplayCondition(self.__class__, rule['PropertyName']) == False:
                        continue
                    message = self.getErrorMessage(rule['ErrorMessage'])
                    if message != None and len(message) > 0:
                        pushError(message, errors)
                except:
                    pushError(str(sys.exc_info()[0]), errors)
            if errors == None or len(errors) == 0:
                return ''
            return '\n'.join(errors)

        def getSingleError(self, propertyName):
            result = ''
            filteredRules = dict(filter(lambda item: str(propertyName) in item[0], self.__ruleCollection.items()))
            if len(filteredRules) == 0:
                return result
            for rule in filteredRules.values():
                try:
                    if rule['Rule']() == True:
                        continue
                    message = self.getErrorMessage(rule['ErrorMessage'])
                    if message != None and len(message) > 0:
                        result =  result + message + '\n'
                except:
                    return str(sys.exc_info()[0])
            return result.strip()
    
    return _TapPlugin
@PluginType
class TestStep(TapPlugin(PythonStep)):
    """Inherit from this class to implement a test step plugin. This could for example be setting up and executing a channel power measurement on a DUT."""

    def Run(self):
        """ Called a number of times per step in the plan during the execution. 
The specific number of times depends on how the step is inserted into the test plan. It might be multiple times if the step is inserted inside a sweep loop step or other control flow related steps."""
        pass

    def PrePlanRun(self):
        """ Called once when the plan starts. """
        pass
    def PostPlanRun(self):
        """ Called once when the plan completes. """ 
        pass
@PluginType
class Dut(TapPlugin(PythonDut)):
    """Inherit from this class to implement a DUT plugin. This could abstract the functinality of a cellphone, PA, WLAN dongle, ..."""
    
    def Open(self):
        """ Called before the test plan starts. Normally used to allocate resources needed by the DUT. 
For example by opening a serial port for AT command communication. """
        pass
    def Close(self):
        """ Called after the test plan has completed. Normally used to deallocate resources that were needed by the DUT. 
For example by closing a serial port."""
        pass
@PluginType
class Instrument(TapPlugin(PythonInstrument)):
    """Inherit from this class to implement an instrument. Implement methods to abstract the functionality of the instrument. For example, this could be a power supply."""
    
    def Open(self):
        """ Called before the test plan starts. Normally used to allocate resources needed by the instrument. 
For example by opening a TCP connection to an instrument. """
        pass

    def Close(self):
        """ Called after the test plan has completed. Normally used to deallocate resources that were needed by the instrument. 
For example by closing a TCP connection."""
        pass
@PluginType
class ResultListener(TapPlugin(PythonResultListener)):
    """ Inherit from this class to implement a result listener. 
Result listeners are used to log data generated when the test plan runs.
Note, most of the methods on this class are invoked asynchronously from the test plan. 
They are guaranteed to be called from the same thread in the order specified in the TAP Developer guide."""
    
    def OnTestPlanRunStart(self, planRun):
        """ Called when the plan starts. """
        pass

    def OnTestStepRunStart(self, stepRun):
        """ Called when a test starts. """
        pass

    def OnResultPublished(self, stepRun, result):
        """ Called when a result has been published from a step. """
        pass

    def OnTestStepRunCompleted(self, stepRun):
        """ Called when as test has completed. """
        pass

    def OnTestPlanRunCompleted(self, planRun, logStream):
        """ Called when the test plan has completed. """
        pass

    def Open(self):
        """ Called before the test plan starts. Normally used to allocate resources needed by the result listener. 
For example by opening a database connection. """
        pass

    def Close(self):
        """ Called after the test plan has completed. Normally used to deallocate resources that were needed by the result listener. 
For example by closing a database connection."""
        pass
@PluginType
class ComponentSettings(TapPlugin(PythonComponentSettings)):
    """ Describes a settings class. Add properties to be able to configure it from the GUI. 
        Use the static method GetCurrent to access the current values.
    """    

    @classmethod
    def GetCurrent(cls):
        """Gets the current settings instance. This value changes when new settings profiles are loaded."""
        try: 
            return OpenTap.ComponentSettings.GetCurrent(cls.__WrapperType__)
        except AttributeError: # The component settings has not yet been loaded so __WrapperType__ is not set.
            PythonComponentSettings.EnsureLoaded(cls);
            return OpenTap.ComponentSettings.GetCurrent(cls.__WrapperType__)
        

    def reload(self):
        cls = self.__class__
        self.__class__ = sys.modules[type(self).__module__].__dict__[type(self).__name__] 
        self.__class__.__WrapperType__ = cls.__WrapperType__

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
sys.stderr = Logger(OpenTap.LogEventType.Error);
class Property:
    """ A property is a variable on an object. This class descripbes such a variable, including its name, type, default value and attributes."""
    def __init__(self, name, value, _type):
        if(_type == None):
            _type = type(value)
        self.Name = name
        self.Value = value
        self.Type = _type
        self.Attributes = {}
    """ Adds a new attribute to a property. The attribute must be a .NET attribute. Like OpenTap.DisplayAttribute. Arguments must be simple types, like numbers and strings."""
    def AddAttribute(self, attribute, *args, **kwargs):
        self.Attributes[attribute] = (attribute, args, kwargs)
        return self;

class Method:
    def __init__(self, name, returnType):
        self.Name = name
        self.Attributes = {}
        self.ReturnType = returnType
        self.Arguments = []

    def AddAttribute(self, attribute, *args, **kwargs):
        self.Attributes[attribute] = (attribute, args, kwargs)
        return self;

    def AddArgument(self, argumentName, argumentType):
        self.Arguments.append((argumentName, argumentType))
        return self;

class_properties = {}

def _addClassProperty(cls, name, value, type):
    if not (cls in class_properties):
        class_properties[cls] = {};
    prop = Property(name, value, type)
    class_properties[cls][name] = prop
    return prop

def GetClassProperties(cls):
    if not (cls in class_properties):
        return []
    else:
        return list(class_properties[cls].values())

def GetClassPropertyType(cls, propertyName):
    for prop in GetClassProperties(cls):
        if(prop.Name == propertyName):
            return prop.Type
    return []

class_methods = {}

def _addClassMethod(cls, name, returnType):
    if not (cls in class_methods):
        class_methods[cls] = {};
    method = Method(name, returnType)
    class_methods[cls][name] = method
    return method

def GetClassMethods(cls):
    if not (cls in class_methods):
        return[]
    else:
        return list(class_methods[cls].values())


def GetClassInterfaces(cls):
    out = []
    for base in cls.__bases__:
        out.extend(GetClassInterfaces(base))
    if not (cls in class_interfaces):
        return out
    out.extend(class_interfaces[cls])
    return out

def GetEnumMembers(enumCls):
    return enumCls._member_names_

def GetEnumValue(enumCls, enumMemberName):
    return enumCls[enumMemberName].value

def GetEnumMemberFromIndex(self, propertyName, enumIndex):
    for prop in GetClassProperties(self.__class__):
        if prop.Type is type(getattr(self, propertyName)):
            return list(prop.Type.__members__.values())[enumIndex-1]

def GetEnumMemberFromClsDict(enumCls, value):
    atttrNames = [attrName for attrName in enumCls.__dict__ if not attrName.startswith('__') and not attrName.endswith('_')]
    for attr in atttrNames:
        if(attr == value):
            return attr
if sys.version_info[0] == 3:
     class AutoNumber(Enum):
         def __new__(cls):
             value = len(cls.__members__) + 1
             obj = object.__new__(cls)
             obj._value_ = value
             return obj
         
attribute_lookup = {}

def Attribute(tap_attr, *args, **kwargs):
    """ This decorator function defines a attribute for a class. """
    def attr_attach(x):
        if not x in attribute_lookup:
            attribute_lookup[x] = []
        attribute_lookup[x].append((tap_attr, args, kwargs))
        return x
    return attr_attach

abstract_lookup = {}

def Abstract(cls):
    abstract_lookup[cls] = True
    return cls

def IsAbstract(cls):
    return abstract_lookup.get(cls) == True

if sys.version_info[0] == 3:
    def isenum(type):
        return issubclass(type, Enum);
    
    def _get_tap_enums(items):
        for item in items:
            if type(item)==enum.EnumMeta:
                yield item
            elif type(item)==enum.IntEnum:
                yield item
else:
    def isenum(type):
        return False
    def _get_tap_enums(items):
        return []

def get_tap_enums(items):
    return list(_get_tap_enums(items))

def get_attributes(cls):
    if cls in attribute_lookup:
        return attribute_lookup[cls]
    return []

def to_list(items):
    return list(items)

def add_search_directories(dirs):
    for dir in dirs:
        sys.path.append(dir)

def base_class_name(cls):
    basecls = cls.__bases__[0]
    return basecls.__module__ + "." + basecls.__name__


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
    return __import__(modname);

