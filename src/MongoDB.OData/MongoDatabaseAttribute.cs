using System;

namespace MongoDB.OData
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class MongoDatabaseAttribute : Attribute
    {
        public string Name { get; private set; }

        public MongoDatabaseAttribute(string name)
        {
            Name = name;
        }
    }
}
