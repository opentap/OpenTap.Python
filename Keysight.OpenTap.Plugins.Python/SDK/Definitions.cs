//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Keysight.OpenTap.Plugins.Python
{
    [Obfuscation(Exclude = true)]
    public class AttributeDefinition
    {
        public Type AttributeType;                                                         
        public object[] AttributeArgs;
    }
    [Obfuscation(Exclude = true)]
    public class PropertyDefinition
    {
        public readonly string Name;
        public readonly Type Type;
        public List<AttributeDefinition> Attributes = new List<AttributeDefinition>();
        public PropertyDefinition(string name, Type type)
        {
            this.Name = name;
            this.Type = type;
        }

        [Obfuscation(Exclude = true)]
        public void AddAttribute(Type AttributeType, object[] args)
        {
            Attributes.Add(new AttributeDefinition() { AttributeType = AttributeType, AttributeArgs = args });
        }
    }
}