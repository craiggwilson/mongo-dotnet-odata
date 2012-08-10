using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Services.Providers;
using MongoDB.Bson.Serialization;

namespace MongoDB.OData
{
    internal class TypedMongoResourcePropertyAnnotation
    {
        private readonly BsonMemberMap _memberMap;
        
        public BsonMemberMap MemberMap
        {
            get { return _memberMap; }
        }

        public TypedMongoResourcePropertyAnnotation(BsonMemberMap memberMap)
	    {
            _memberMap = memberMap;
	    }
    }
}