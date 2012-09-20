using MongoDB.Driver;
using System;
using System.Linq;

namespace MongoDB.OData.Typed
{
    internal class TypedResourceSetAnnotation
    {
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly Func<TypedDataSource, MongoCollection> _getCollection;
        private readonly Func<TypedDataSource, IQueryable> _getQueryableRoot;
        private readonly Action<TypedDataSource> _setDataContext;

        public TypedResourceSetAnnotation(string databaseName, string collectionName, Func<TypedDataSource, MongoCollection> getCollection, Func<TypedDataSource, IQueryable> getQueryableRoot, Action<TypedDataSource> setDataContext)
        {
            _collectionName = collectionName;
            _databaseName = databaseName;
            _getCollection = getCollection;
            _getQueryableRoot = getQueryableRoot;
            _setDataContext = setDataContext ?? (_ => { });
        }
        
        public string CollectionName
        {
            get { return _collectionName; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public Func<TypedDataSource, MongoCollection> GetCollection
        {
            get { return _getCollection; }
        }

        public Func<TypedDataSource, IQueryable> GetQueryableRoot
        {
            get { return _getQueryableRoot; }
        }

        public Action<TypedDataSource> SetDataContext
        {
            get { return _setDataContext; }
        }
    }
}