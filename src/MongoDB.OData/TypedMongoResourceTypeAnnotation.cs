using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Services.Providers;
using MongoDB.Bson.Serialization;

namespace MongoDB.OData
{
    internal class TypedMongoResourceTypeAnnotation
    {
        private readonly BsonClassMap _classMap;
        private readonly List<ResourceType> _derivedTypes;
        private readonly TypedMongoResourceSetAnnotation _set;

        public BsonClassMap ClassMap
        {
            get { return _classMap; }
        }

        public string CollectionName
        {
            get { return _set.CollectionName; }
        }

        public string DatabaseName
        {
            get { return _set.DatabaseName; }
        }

        public IEnumerable<ResourceType> DerivedTypes
        {
            get { return _derivedTypes; }
        }
        
        public bool HasDerivedTypes
        {
            get { return _derivedTypes.Any(); }
        }

        public TypedMongoResourceTypeAnnotation(TypedMongoResourceSetAnnotation set, BsonClassMap classMap)
	    {
            _classMap = classMap;
            _derivedTypes = new List<ResourceType>();
            _set = set;
	    }

        public void AddDerivedTyped(ResourceType type)
        {
            _derivedTypes.Add(type);
        }
    }
}