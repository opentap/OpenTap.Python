using System;
using Python.Runtime;

namespace OpenTap.Python;

class PythonDescribeValueAnnotation : IDisplayAnnotation
{
    readonly AnnotationCollection annotations;
    public PythonDescribeValueAnnotation(AnnotationCollection annotations)
    {
        this.annotations = annotations;
    }
    public string Description {
    
        get
        {
            var val = annotations.Get<IObjectValueAnnotation>().Value as PyObject;
            if (val != null)
                return val.InvokeMethod("describe").ToString();
            return annotations.Get<IDisplayAnnotation>(false, this).Description;
        }
    }

    public string[] Group { get; } = Array.Empty<string>();
    public string Name => annotations.Get<IDisplayAnnotation>(false, this).Name;
    public double Order { get; }
    public bool Collapsed { get; }
}