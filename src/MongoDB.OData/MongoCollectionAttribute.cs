using System;

namespace MongoDB.OData
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class MongoCollectionAttribute : Attribute
    {
        public string Name { get; private set; }

        public MongoCollectionAttribute(string name)
        {
            Name = name;
        }
    }
}
