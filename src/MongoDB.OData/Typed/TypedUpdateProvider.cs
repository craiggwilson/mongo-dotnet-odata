using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Text;

namespace MongoDB.OData.Typed
{
    internal class TypedUpdateProvider : IDataServiceUpdateProvider
    {
        private readonly List<Action> _actions;
        private readonly List<object> _rememberedInstances;
        private readonly TypedMetadata _metadata;
        private readonly TypedDataSource _currentDataSource;

        public TypedUpdateProvider(TypedDataSource currentDataSource, TypedMetadata metadata)
        {
            _currentDataSource = currentDataSource;
            _metadata = metadata;
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
            if (!_metadata.TryResolveResourceType(fullTypeName, out resourceType))
            {
                throw new NotSupportedException(string.Format("Type {0} not found", fullTypeName));
            }

            var annotation = (TypedResourceTypeAnnotation)resourceType.CustomState;
            var instance = annotation.ClassMap.CreateInstance();

            var collection = GetCollection(resourceType);
            if (collection != null)
            {
                _actions.Add(() => collection.Insert(resourceType.InstanceType, instance));
            }
            _rememberedInstances.Add(instance);

            return instance;
        }

        public void DeleteResource(object targetResource)
        {
            var resourceType = GetResourceType(targetResource);
            var collection = GetCollection(resourceType);
            var annotation = (TypedResourceTypeAnnotation)resourceType.CustomState;

            var idValue = annotation.ClassMap.IdMemberMap.Getter(targetResource);
            var idSerializer = annotation.ClassMap.IdMemberMap.GetSerializer();
            var doc = new BsonDocument();
            using (var writer = new BsonDocumentWriter(doc))
            {
                writer.WriteStartDocument();
                writer.WriteName("_id");
                idSerializer.Serialize(BsonSerializationContext.CreateRoot(writer), idValue);
                writer.WriteEndDocument();
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
                if (!_metadata.TryResolveResourceType(fullTypeName, out type))
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
            var resourceType = GetResourceType(targetResource);
            var annotation = (TypedResourceTypeAnnotation)resourceType.CustomState;
            var memberMap = annotation.ClassMap.GetMemberMap(propertyName);
            return memberMap.Getter(targetResource);
        }

        public void RemoveReferenceFromCollection(object targetResource, string propertyName, object resourceToBeRemoved)
        {
            throw new NotImplementedException();
        }

        public object ResetResource(object targetResource)
        {
            var resourceType = GetResourceType(targetResource);
            var template = CreateResource(null, resourceType.FullName);
            var annotation = (TypedResourceTypeAnnotation)resourceType.CustomState;

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
            var resourceType = GetResourceType(targetResource);
            var annotation = (TypedResourceTypeAnnotation)resourceType.CustomState;
            var memberMap = annotation.ClassMap.GetMemberMap(propertyName);
            var serializer = memberMap.GetSerializer() as IBsonArraySerializer;
            if (serializer != null)
            {
                BsonSerializationInfo itemSerializationInfo;
                if (serializer.TryGetItemSerializationInfo(out itemSerializationInfo))
                {
                    var array = itemSerializationInfo.SerializeValues((IEnumerable)propertyValue);
                    var memberMapSerializationInfo = new BsonSerializationInfo(memberMap.ElementName,
                        serializer,
                        memberMap.MemberType);
                    propertyValue = memberMapSerializationInfo.DeserializeValue(array);
                }
            }
            memberMap.Setter(targetResource, propertyValue);
            if (_rememberedInstances.Contains(targetResource))
            {
                return;
            }

            var collection = GetCollection(resourceType);
            _rememberedInstances.Add(targetResource);
            _actions.Add(() => collection.Save(resourceType.InstanceType, targetResource));
        }

        private MongoCollection GetCollection(ResourceType resourceType)
        {
            while (resourceType.BaseType != null)
            {
                resourceType = resourceType.BaseType;
            }

            var resourceSet = _metadata.ResourceSets.SingleOrDefault(x => x.ResourceType == resourceType);
            if (resourceSet == null)
            {
                return null;
            }

            var state = (TypedResourceSetAnnotation)resourceSet.CustomState;
            return state.GetCollection(_currentDataSource);
        }

        private ResourceType GetResourceType(object targetResource)
        {
            return _metadata.Types.Single(x => x.InstanceType == targetResource.GetType());
        }
    }
}