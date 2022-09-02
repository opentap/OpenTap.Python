using System;
using System.Xml.Linq;
using Python.Runtime;

namespace OpenTap.Python;

/// <summary>
/// Uses Python pickle to serialize any PyObject to base64 and back.
/// This might break in some cases, but it is the best guess we can do.
/// This is for example used for enums.
/// </summary>
public class PyObjectSerializer : ITapSerializerPlugin
{
    public bool Deserialize(XElement node, ITypeData t, Action<object> setter)
    {
        if (t.DescendsTo(typeof(PyObject)))
        {
            using (Py.GIL())
            {
                
                var pickle = Py.Import("pickle");
                var codecs = Py.Import("codecs");
                
                //pickle.loads(codecs.decode(node.value.encode(), "base64"))
                var val = pickle.InvokeMethod("loads", codecs.InvokeMethod("decode", node.Value.ToPython().InvokeMethod("encode"), "base64".ToPython()));
                
                setter(val);
            }

            return true;
        }
        return false;
    }

    public bool Serialize(XElement node, object obj, ITypeData expectedType)
    {
        if (obj is PyObject py)
        {
            using (Py.GIL())
            {
                var pickle = Py.Import("pickle");
                var codecs = Py.Import("codecs");
                // codecs.encode(pickle.dumps(obj), "base64").decode()
                var d = codecs.InvokeMethod("encode", pickle.InvokeMethod("dumps", py, 0.ToPython()),
                    "base64".ToPython());
                node.SetValue(d.InvokeMethod("decode"));
            }
        }

        return false;
    }

    public double Order { get; }
}