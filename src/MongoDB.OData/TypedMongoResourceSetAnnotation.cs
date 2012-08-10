using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Services.Providers;

namespace MongoDB.OData
{
    internal class TypedMongoResourceSetAnnotation
    {
        private readonly string _collectionName;
        private readonly string _databaseName;
        
        public string CollectionName
        {
            get { return _collectionName; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public TypedMongoResourceSetAnnotation(string databaseName, string collectionName)
	    {
            _collectionName = collectionName;
            _databaseName = databaseName;
	    }
    }
}