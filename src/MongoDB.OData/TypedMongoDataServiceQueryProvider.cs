using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Services.Providers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MongoDB.OData
{
    internal class TypedMongoDataServiceQueryProvider : IDataServiceQueryProvider
    {
        private readonly TypedMongoDataServiceMetadataProvider _metadata;
        private MongoServer _server;

        public object CurrentDataSource
        {
            get { return _server; }
            set { _server = value as MongoServer; }
        }

        public bool IsNullPropagationRequired
        {
            get { return false; }
        }

        public TypedMongoDataServiceQueryProvider(TypedMongoDataServiceMetadataProvider metadata)
        {
            _metadata = metadata;
        }

        public object GetOpenPropertyValue(object target, string propertyName)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<KeyValuePair<string, object>> GetOpenPropertyValues(object target)
        {
            throw new NotImplementedException();
        }

        public object GetPropertyValue(object target, ResourceProperty resourceProperty)
        {
            throw new NotImplementedException();
        }

        public IQueryable GetQueryRootForResourceSet(ResourceSet resourceSet)
        {
            var annotation = (TypedMongoResourceSetAnnotation)resourceSet.CustomState;
            var resourceType = resourceSet.ResourceType;

            return GetQueryableCollection(resourceType);
        }

        public ResourceType GetResourceType(object target)
        {
            return _metadata.Types.Single(x => x.InstanceType == target.GetType());
        }

        public object InvokeServiceOperation(ServiceOperation serviceOperation, object[] parameters)
        {
            throw new NotImplementedException();
        }

        public MongoCollection GetCollection(ResourceType resourceType)
        {
            var annotation = (TypedMongoResourceTypeAnnotation)resourceType.CustomState;
            var db = _server.GetDatabase(annotation.DatabaseName);

            var instanceType = resourceType.InstanceType;

            var genericMethod = typeof(MongoDatabase).GetMethods()
                .Where(x => x.Name == "GetCollection")
                .Where(x => x.IsGenericMethod)
                .Single(x => x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(string));

            var method = genericMethod.MakeGenericMethod(instanceType);

            return (MongoCollection)method.Invoke(db, new[] { annotation.CollectionName });
        }

        private IQueryable GetQueryableCollection(ResourceType resourceType)
        {
            var collection = GetCollection(resourceType);
            var instanceType = resourceType.InstanceType;

            var genericMethod = typeof(LinqExtensionMethods).GetMethod("AsQueryable", new[] { typeof(MongoCollection) });

            var method = genericMethod.MakeGenericMethod(instanceType);

            return (IQueryable)method.Invoke(null, new[] { collection });
        }
    }
}