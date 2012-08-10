using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Services.Providers;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace MongoDB.OData
{
    internal class TypedMongoDataServiceUpdateProvider : IDataServiceUpdateProvider
    {
        private readonly List<Action> _actions;
        private readonly List<object> _rememberedInstances;
        private readonly TypedMongoDataServiceMetadataProvider _metadataProvider;
        private readonly TypedMongoDataServiceQueryProvider _queryProvider;

        private MongoServer CurrentDataSource
        {
            get { return _queryProvider.CurrentDataSource as MongoServer; }
        }

        public TypedMongoDataServiceUpdateProvider(TypedMongoDataServiceMetadataProvider metadataProvider, TypedMongoDataServiceQueryProvider queryProvider)
        {
            _metadataProvider = metadataProvider;
            _queryProvider = queryProvider;
            _actions = new List<Action>();
            _rememberedInstances = new List<object>();
        }

        public void AddReferenceToCollection(object targetResource, string propertyName, object resourceToBeAdded)
        {
            throw new NotImplementedException();
        }

        public void ClearChanges()
        {
            _actions.Clear();
            _rememberedInstances.Clear();
        }

        public object CreateResource(string containerName, string fullTypeName)
        {
            ResourceType resourceType;
            if (!_metadataProvider.TryResolveResourceType(fullTypeName, out resourceType))
            {
                throw new NotSupportedException(string.Format("Type {0} not found", fullTypeName)); 
            }

            var annotation = (TypedMongoResourceTypeAnnotation)resourceType.CustomState;
            var instance = annotation.ClassMap.CreateInstance();

            var collection = _queryProvider.GetCollection(resourceType);

            _actions.Add(() => collection.Insert(resourceType.InstanceType, instance));
            _rememberedInstances.Add(instance);

            return instance;
        }

        public void DeleteResource(object targetResource)
        {
            var resourceType = _metadataProvider.Types.Single(x => x.InstanceType == targetResource.GetType());
            var annotation = (TypedMongoResourceTypeAnnotation)resourceType.CustomState;
            var collection = _queryProvider.GetCollection(resourceType);

            var idValue = annotation.ClassMap.IdMemberMap.Getter(targetResource);
            var idSerializer = annotation.ClassMap.IdMemberMap.GetSerializer(idValue.GetType());
            var doc = new BsonDocument();
            using (var writer = BsonWriter.Create(doc))
            {
                writer.WriteName("_id");
                idSerializer.Serialize(writer, annotation.ClassMap.IdMemberMap.MemberType, idValue, annotation.ClassMap.IdMemberMap.SerializationOptions);
            }

            _rememberedInstances.Add(targetResource);
            _actions.Add(() => collection.Remove(Query.EQ("_id", doc[0])));
        }

        public object GetResource(IQueryable query, string fullTypeName)
        {
            var enumerator = query.GetEnumerator();
            if (!enumerator.MoveNext())
                throw new NotSupportedException("Resource not found");
            var resource = enumerator.Current;
            if (enumerator.MoveNext())
                throw new NotSupportedException("Resource not uniquely identified");

            if (fullTypeName != null)
            {
                ResourceType type;
                if (!_metadataProvider.TryResolveResourceType(fullTypeName, out type))
                {
                    throw new NotSupportedException("ResourceType not found");
                }
                if (!type.InstanceType.IsAssignableFrom(resource.GetType()))
                {
                    throw new NotSupportedException("Unexpected resource type");
                }
            }
            return resource; 
        }

        public object GetValue(object targetResource, string propertyName)
        {
            var resourceType = _metadataProvider.Types.Single(x => x.InstanceType == targetResource.GetType());
            var annotation = (TypedMongoResourceTypeAnnotation)resourceType.CustomState;
            var memberMap = annotation.ClassMap.GetMemberMap(propertyName);
            return memberMap.Getter(targetResource);
        }

        public void RemoveReferenceFromCollection(object targetResource, string propertyName, object resourceToBeRemoved)
        {
            throw new NotImplementedException();
        }

        public object ResetResource(object targetResource)
        {
            var resourceType = _metadataProvider.Types.Single(x => x.InstanceType == targetResource.GetType());
            var template = CreateResource(null, resourceType.FullName);
            var annotation = (TypedMongoResourceTypeAnnotation)resourceType.CustomState;

            var idMemberMap = annotation.ClassMap.IdMemberMap;

            idMemberMap.Setter(template, idMemberMap.Getter(targetResource));

            return template;
        }

        public object ResolveResource(object resource)
        {
            //we are passed the value we returned from GetResource
            return resource;
        }

        public void SaveChanges()
        {
            _actions.ForEach(a => a());
        }

        public void SetConcurrencyValues(object resourceCookie, bool? checkForEquality, IEnumerable<KeyValuePair<string, object>> concurrencyValues)
        {
            throw new NotImplementedException();
        }

        public void SetReference(object targetResource, string propertyName, object propertyValue)
        {
            throw new NotImplementedException();
        }

        public void SetValue(object targetResource, string propertyName, object propertyValue)
        {
            var resourceType = _metadataProvider.Types.Single(x => x.InstanceType == targetResource.GetType());
            var annotation = (TypedMongoResourceTypeAnnotation)resourceType.CustomState;
            var memberMap = annotation.ClassMap.GetMemberMap(propertyName);
            memberMap.Setter(targetResource, propertyValue);
            if (_rememberedInstances.Contains(targetResource))
                return;

            var collection = _queryProvider.GetCollection(resourceType);
            _rememberedInstances.Add(targetResource);
            _actions.Add(() => collection.Save(resourceType.InstanceType, targetResource));
        }
    }
}