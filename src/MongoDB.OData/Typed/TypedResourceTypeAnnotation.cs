using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.OData.Typed
{
    public class TypedResourceTypeAnnotation
    {
        private readonly BsonClassMap _classMap;

        public TypedResourceTypeAnnotation(BsonClassMap classMap)
        {
            _classMap = classMap;
        }

        public BsonClassMap ClassMap
        {
            get { return _classMap; }
        }
    }
}