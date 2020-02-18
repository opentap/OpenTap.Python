//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
using Python.Runtime;
using System;
using System.Reflection;
using System.Collections;

namespace Keysight.Plugins.Python
{
    /// <summary> Convert for various IList types. </summary>
    [Obfuscation(Exclude = true)]
    public class ListPythonConverter : IPythonConverter
    {
        public double Order => 1;

        public object FromPythonObject(PyObject value, Type type)
        {
            if (type.IsArray)
                return value.AsManagedObject(type);
            Type genericType = typeof(object);
            if (typeof(IList).IsAssignableFrom(type))
            {
                var lst = (IList) Activator.CreateInstance(type);
                var pylist = new PyList(value);
                foreach(PyObject item in pylist)
                {
                    var result = PythonConverter.FromPythonObject(item, genericType);
                    lst.Add(result);
                }
                return lst;
            }
            return UnableToConvert.Instance;
        }

        public PyObject ToPythonObject(object value)
        {
            if(value is IList)
                return value.ToPython();
            return null;
        }
    }

}
