using System;

namespace MongoDB.OData
{
    /// <summary>
    /// Defines the mongodb collection to use.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class MongoCollectionAttribute : Attribute
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoCollectionAttribute" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public MongoCollectionAttribute(string name)
        {
            Name = name;
        }
    }
}
