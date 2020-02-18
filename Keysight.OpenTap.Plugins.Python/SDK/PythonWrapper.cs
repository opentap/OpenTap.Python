//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
using Python.Runtime;
using System;
using System.Reflection;
using System.ComponentModel;
using System.Xml.Serialization;
using OpenTap;
using Keysight.OpenTap.Plugins.Python;

namespace Keysight.Plugins.Python
{
    [Obfuscation(Exclude = true)]
    public interface IPythonWrapper
    {
        PyObject PythonObject { get; set; }
        void load_instance();
    }

    /// <summary>
    /// Base class for python wrapped objects.
    /// </summary>
    [Obfuscation(Exclude = true)]
    public abstract class PythonWrapper: ValidatingObject,  IPythonWrapper, IDataErrorInfo
    {
        static PythonWrapper()
        {
            WrapperBuilder.LoadPython();
        }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
        
        public class PythonNameAttribute : Attribute
        {
            public string PythonName;
            public PythonNameAttribute(string PythonName)
            {
                this.PythonName = PythonName;
            }
        }
        
        /// <summary>
        ///   Points to the base class that a wrapper type wraps.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        public class WrappedTypeAttribute: Attribute
        {
            public Type WrappedBaseType;
            public WrappedTypeAttribute(Type wrappedType)
            {
                WrappedBaseType = wrappedType;
            }
        }
        [ColumnDisplayName("Step Type", Order: 1)]
        [Browsable(false)]
        [XmlIgnore]
        public string TypeName
        {
            get
            {
                var disp = GetType().GetCustomAttribute<DisplayAttribute>();
                if (disp == null)
                    return GetType().Name;
                
                return disp.GetFullName();
            }
        }
        
        public PythonWrapper()
        {
            load_instance();
        }

        /// <summary> Overridden by the class build bu WrapperBuilder </summary>
        public abstract void load_instance();

        public static void Load(IPythonWrapper wrapper)
        {
            wrapper.load_instance();
        }

        /// <summary>
        /// Loads python code and creates a PythonStep object using the provided name as the name of the class to create a PythonStep from.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="moduleName"></param>
        virtual protected void load(string name, string moduleName)
        {
            try
            {
                PythonWrapperExtensions.Load(this, name, moduleName);
                using (Py.GIL())
                {
                    var validatingObject = pyobj.AsManagedObject(typeof(IValidatingObject)) as IValidatingObject;
                    if (validatingObject != null)
                        validatingObject.PropertyChanged += Step_PropertyChanged;
                }
            }catch(PythonException pe)
            {
                WrapperBuilder.printPythonException(pe);
            }
            catch
            {
                
            }

            if (!rulesLoaded)
            {
                // Create rules for each declared property, since we cannot override the behavior of ValidatingObject.
                rulesLoaded = true;
                foreach (var property in TypeData.FromType(GetType()).GetMembers())
                    Rules.Add(() => string.IsNullOrWhiteSpace(getError(property.Name)) == true, () => getError(property.Name), property.Name);
            }
        }

        bool rulesLoaded = false;

        private void Step_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }
                
        PyObject pyobj = null;
        [Browsable(false)]
        [XmlIgnore]
         protected PyObject PythonObject
        {
            get => pyobj;
            set
            {
                pyobj = value;
                IPythonProxy pyproxy = pyobj.AsManagedObject(typeof(object)) as IPythonProxy;
                if (pyproxy != null)
                    pyproxy.Wrapper = this;
            }
        }

        PyObject IPythonWrapper.PythonObject
        {
            get => PythonObject;
            set => PythonObject = value;
        }

        string getError(string propertyName = null)
        {
            string result = "";
            PyThread.Invoke(() =>
            {
                PyObject errors = propertyName == null ? PythonObject.InvokeMethod("getError") : PythonObject.InvokeMethod("getSingleError", propertyName.ToPython());
                result = errors.ToString();
            }, true);
            return result;
        }
    }
}
