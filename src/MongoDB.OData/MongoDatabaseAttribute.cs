using System;

namespace MongoDB.OData
{
    /// <summary>
    /// Defines the mongodb database to use.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class MongoDatabaseAttribute : Attribute
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDatabaseAttribute" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public MongoDatabaseAttribute(string name)
        {
            Name = name;
        }
    }
}
