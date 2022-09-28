using System.Collections.Generic;
using Python.Runtime;

namespace OpenTap.Python;

// PyObject properties can be anything, but 
// we have modified PythonNet to add PythonTypeAttribute
// to properties which are python types, so we are able
// to resolve the type and make decisions on how to handle them.
// Currently only enum-types are supported.
public class PyObjectAnnotator : IAnnotator
{
    public void Annotate(AnnotationCollection annotations)
    {
        var mem = annotations.Get<IMemberAnnotation>();
        if (mem == null) return;
        if (mem.ReflectionInfo.DescendsTo(typeof(PyObject)) == false) return;
        var pythonTypeAttr = mem.Member.GetAttribute<PythonTypeAttribute>();
        if (pythonTypeAttr == null) return;
        using (Py.GIL())
        {
            PyObject mod = Py.Import(pythonTypeAttr.Module);
            var type = new PyType(mod.GetAttr(pythonTypeAttr.TypeName));
            if (type.HasAttr("_member_map_"))
            {
                // Handling enum: enums has _member_map_.
                var member_map = type.GetAttr("_member_map_");
                try
                {
                    // in some pythons this is not a dict but a OrderedDict.
                    // so we have to manually pull the values
                    var list = new List<PyObject>();
                    using var values = new PyIterable(member_map.InvokeMethod("values"));
                    foreach (var pair in values)
                        list.Add(type.GetAttr(pair.GetAttr("name")));

                    annotations.Add(new PythonEnumValueProperty(list));
                }
                catch
                {
                    
                }
            }

            // classes that implement describe() can provide a description string for a value.
            // see for example EnumUsage.
            if (type.HasAttr("describe"))
                annotations.Add(new PythonDescribeValueAnnotation(annotations));
        }

    }

    public double Priority => 0.0;
}