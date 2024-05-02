# Migrating from 2.X to 3.0

Some breaking changes has been introduced from 2.4 to 3.0 in order to streamline the developer experience.

## OpenTAP integration file
```python
## PREVIOUSLY
import PythonTap
import OpenTap # import C# OpenTAP

## NOW
import opentap # note, not capitalized to follow python naming convention.
import OpenTap # This follows the C# naming convention

```

## Test Steps (And other plugins)

### Defining Properties
Previously the test step properties got defined in the __init__ method. This has been changed to be defined in the class it self:
```python
## PREVIOUSLY:
class BasicFunctionality(TestStep):
    def __init__(self):
        # [...]
        prop = self.AddProperty("Frequency", 1e9, Double)
        prop.AddAttribute(OpenTap.UnitAttribute, "Hz")

## NOW:
class BasicFunctionality(TestStep):
    Frequency = property(Double, 1e9).\
        add_attribute(Unit("Hz"))
```

### Validation Rules
```python
## PREVIOUSLY
self.AddRule(lambda: self.Frequency >= 0, lambda: '{} Hz is an invalid value. Frequency must not be negative'.format(self.Frequency), "Frequency")
## NOW
self.Rules.Add(opentap.Rule("Frequency", lambda: self.Frequency >= 0, lambda: '{} Hz is an invalid value. Frequency must not be negative'.format(self.Frequency)))
```

### Attribute is renamed to attribute and simplified
```python
## PREVIOUSLY
@Attribute(OpenTap.DisplayAttribute, "Basic Functionality", "description...", "Python Example")
##NOW
@attribute(OpenTap.Display("Basic Functionality", "description...", "Python Example"))
```

### Log Messages
```python
## PREVIOUSLY
self.Info("Info message")
## NOW
self.log.Info("Info message")
```

### Property Changed Notifications

Implementing `__setattr__` is no longer supported. If you want to write properties that depends on eachother use get-only properties. See BasicFunctionality example
```python
## PREVIOUSLY
def __setattr__(self, name, value):
    super(BasicFunctionality, self).__setattr__(name, value)
    if name == "Frequency":
        self.FrequencyIsDefault = abs(self.Frequency - 1e9) < 0.001

## NOW
    @property(Boolean)
    def FrequencyIsDefault(self):
        return abs(self.Frequency - 1e9) < 0.001
```

### Exposing Methods
```python 
## PREVIOUSLY
    def __init__(self):
        # [...]
        resetFrequency = self.RegisterMethod("resetFrequency", None);
        resetFrequency.AddAttribute(BrowsableAttribute, True)
        resetFrequency.AddAttribute(OpenTap.EnabledIfAttribute, "FrequencyIsDefault", False,HideIfDisabled=True)
        resetFrequency.AddAttribute(OpenTap.DisplayAttribute, "Reset Frequency", None)
        
    def resetFrequency(self):
        self.Frequency = 1e9

## NOW
    @attribute(Browsable(True))
    @attribute(OpenTap.EnabledIf("FrequencyIsDefault", False, HideIfDisabled = True))
    @attribute(OpenTap.Display("Reset Frequency", None))
    @method()
    def resetFrequency(self):
        self.Frequency = 1e9
```


