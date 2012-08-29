using MongoDB.Driver;
using System;
using System.Linq;

namespace MongoDB.OData.Typed
{
    internal class TypedResourceSetAnnotation
    {
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly Func<object, IQueryable> _getter;
        private readonly Action<object, MongoServer> _setter;

        public TypedResourceSetAnnotation(string databaseName, string collectionName, Func<object, IQueryable> getter, Action<object, MongoServer> setter)
        {
            _collectionName = collectionName;
            _databaseName = databaseName;
            _getter = getter;
            _setter = setter;
        }
        
        public string CollectionName
        {
            get { return _collectionName; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public Func<object, IQueryable> Getter
        {
            get { return _getter; }
        }

        public Action<object, MongoServer> Setter
        {
            get { return _setter; }
        }
    }
}