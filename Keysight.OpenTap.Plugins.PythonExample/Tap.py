class TestStep:
    pass
class Instrument:
    pass
class Dut:
    pass

units = {}
display = {}
def get_unit(func):
    print func
    return units[func]

def Unit(unit):
    def innerUnit(func):
        units[func] = unit
        print ("Unit:", func, unit)
        return func
        #func()
    return innerUnit

def Display(name):
    def innerUnit(func):
        display[func] = name
        print ("Display:", func, name)
        return func
        #func()
    return innerUnit
