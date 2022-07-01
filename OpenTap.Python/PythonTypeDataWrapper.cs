using System.Collections.Generic;
using Python.Runtime;

namespace OpenTap.Python;

/// <summary>
/// This wrapper is mostly needed because of a problem in pythonnet requiring us to do ToPython.AsManagedObject after
/// creating an instance of the object.
/// </summary>
class PythonTypeDataWrapper : ITypeData
{
    readonly TypeData innerType;
    public PythonTypeDataWrapper(TypeData innerType) => this.innerType = innerType;
    public IEnumerable<object> Attributes => innerType.Attributes;
    public string Name => innerType.Name;
    public IEnumerable<IMemberData> GetMembers() => innerType.GetMembers();

    public IMemberData GetMember(string name) => innerType.GetMember(name);

    public object CreateInstance(object[] arguments)
    {
        var mem = innerType.CreateInstance(arguments);
        using(Py.GIL())
            return mem.ToPython().AsManagedObject(innerType.Type);
    }

    public ITypeData BaseType => innerType;
    public bool CanCreateInstance => innerType.CanCreateInstance;

    public override int GetHashCode()
    {
        return innerType.GetHashCode() * 73210693;
    }

    public override bool Equals(object obj)
    {
        if (obj is PythonTypeDataWrapper pw && pw.innerType == innerType)
            return true;
        return base.Equals(obj);
    }

    public override string ToString()
    {
        return innerType.ToString();
    }
}