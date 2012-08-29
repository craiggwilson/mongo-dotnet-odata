using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Data.Services.Common;
using System.Data.Services.Providers;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MongoDB.OData.Typed
{
    internal class TypedMetadataBuilder
    {
        private readonly Type _dataContextType;
        private readonly Dictionary<ResourceType, List<ResourceType>> _childResourceTypes;
        private readonly Dictionary<Type, ResourceType> _knownResourceTypes;
        private readonly Dictionary<string, ResourceSet> _resourceSets;
        private readonly Queue<Tuple<BsonClassMap, ResourceType>> _unvisitedResourceTypes;

        public TypedMetadataBuilder(Type dataContextType)
        {
            _dataContextType = dataContextType;
            _childResourceTypes = new Dictionary<ResourceType, List<ResourceType>>();
            _knownResourceTypes = new Dictionary<Type, ResourceType>();
            _resourceSets = new Dictionary<string, ResourceSet>();
            _unvisitedResourceTypes = new Queue<Tuple<BsonClassMap, ResourceType>>();
        }

        public TypedMetadata BuildMetadata()
        {
            _childResourceTypes.Clear();
            _knownResourceTypes.Clear();
            _resourceSets.Clear();
            _unvisitedResourceTypes.Clear();

            BuildResourceSets();

            var resourceSets = _resourceSets.Values.Select(x =>
            {
                x.SetReadOnly();
                return x;
            });
            var resourceTypes = _knownResourceTypes.Values.Select(x =>
            {
                x.SetReadOnly();
                return x;
            });

            return new TypedMetadata(_dataContextType.Namespace, _dataContextType.Name, resourceSets, resourceTypes);
        }

        private void BuildResourceSets()
        {
            var queryRootProperties = GetCollectionProperties(_dataContextType);

            var globalDatabaseName = GetMongoDatabaseName(_dataContextType) ?? _dataContextType.Name;
            foreach (var queryRootProperty in queryRootProperties)
            {
                var documentType = GetMongoCollectionElementType(queryRootProperty.PropertyType);

                var resourceType = BuildHierarchyForType(documentType, ResourceTypeKind.EntityType);
                if (resourceType == null)
                {
                    throw new InvalidOperationException(string.Format("Property {0} is an invalid container property.", queryRootProperty));
                }

                foreach (var resourceSet in _resourceSets.Values)
                {
                    var instanceType = resourceSet.ResourceType.InstanceType;
                    if (!instanceType.IsAssignableFrom(documentType))
                    {
                        if (!documentType.IsAssignableFrom(instanceType))
                        {
                            continue;
                        }

                        throw new InvalidOperationException(string.Format("There are multiple MongoCollection Properties for the same type hierarchy including type {0}.", documentType));
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format("There are multiple MongoCollection Properties for the same type hierarchy including type {0}.", documentType));
                    }
                }

                var newResourceSet = new ResourceSet(queryRootProperty.Name, resourceType);
                newResourceSet.CustomState = CreateResourceSetAnnotation(globalDatabaseName, resourceType.InstanceType, _dataContextType, queryRootProperty);
                _resourceSets.Add(newResourceSet.Name, newResourceSet);
            }

            PopulateMetadataForTypes();
        }

        private ResourceType BuildHierarchyForType(Type type, ResourceTypeKind kind)
        {
            var maps = new List<BsonClassMap>();

            var baseClassMap = BsonClassMap.LookupClassMap(type);
            ResourceType entityResourceType = null;
            while (baseClassMap != null && !_knownResourceTypes.TryGetValue(baseClassMap.ClassType, out entityResourceType))
            {
                maps.Add(baseClassMap);
                baseClassMap = baseClassMap.BaseClassMap;
                if (baseClassMap.ClassType == typeof(object))
                {
                    baseClassMap = null;
                }
            }

            if (entityResourceType != null)
            {
                if (entityResourceType.ResourceTypeKind == kind)
                {
                    if (maps.Count == 0)
                    {
                        return entityResourceType;
                    }
                }
                else
                {
                    return null;
                }
            }

            for (int i = maps.Count - 1; i >= 0; i--)
            {
                entityResourceType = CreateResourceType(maps[i], kind, entityResourceType);
            }

            return entityResourceType;
        }

        private void BuildReflectionEpmInfo(ResourceType resourceType)
        {
            if (resourceType.ResourceTypeKind == ResourceTypeKind.EntityType)
            {
                var instanceType = resourceType.InstanceType;
                var attributes = (EntityPropertyMappingAttribute[])instanceType.GetCustomAttributes(typeof(EntityPropertyMappingAttribute), resourceType.BaseType == null);
                for (int i = 0; i < attributes.Length; i++)
                {
                    var attribute = attributes[i];
                    resourceType.AddEntityPropertyMappingAttribute(attribute);
                }
            }
        }

        private void BuildResourceTypeProperties(BsonClassMap classMap, ResourceType resourceType)
        {
            foreach (var memberMap in classMap.DeclaredMemberMaps)
            {
                var memberType = memberMap.MemberType;

                var propertyResourceType = GetPropertyResourceType(memberMap);

                ResourcePropertyKind propertyKind;
                switch (propertyResourceType.ResourceTypeKind)
                {
                    case ResourceTypeKind.Collection:
                        propertyKind = ResourcePropertyKind.Collection;
                        break;
                    case ResourceTypeKind.ComplexType:
                        propertyKind = ResourcePropertyKind.ComplexType;
                        break;
                    case ResourceTypeKind.Primitive:
                        propertyKind = ResourcePropertyKind.Primitive;
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Invalid ResourceTypeKind({0}) for a member.", propertyResourceType.ResourceTypeKind));
                }

                if (resourceType.ResourceTypeKind == ResourceTypeKind.EntityType && memberMap == classMap.IdMemberMap && resourceType.BaseType == null)
                {
                    propertyKind |= ResourcePropertyKind.Key;
                }

                var resourceProperty = new ResourceProperty(memberMap.MemberName, propertyKind, propertyResourceType);
                resourceType.AddProperty(resourceProperty);
            }
        }

        private ResourceType CreateResourceType(BsonClassMap classMap, ResourceTypeKind kind, ResourceType baseType)
        {
            var type = classMap.ClassType;
            var resourceType = new ResourceType(type, kind, baseType, type.Namespace, GetResourceTypeName(type), type.IsAbstract);
            resourceType.IsOpenType = false;
            _knownResourceTypes.Add(classMap.ClassType, resourceType);
            _childResourceTypes.Add(resourceType, null);
            if (baseType != null)
            {
                if (_childResourceTypes[baseType] == null)
                {
                    _childResourceTypes[baseType] = new List<ResourceType>();
                }
                _childResourceTypes[baseType].Add(resourceType);
            }

            _unvisitedResourceTypes.Enqueue(Tuple.Create(classMap, resourceType));
            return resourceType;
        }

        private ResourceType GetPropertyResourceType(BsonMemberMap memberMap)
        {
            var serializer = memberMap.GetSerializer(memberMap.MemberType);
            return GetNonEntityResourceType(memberMap.MemberType, serializer);
        }

        private ResourceType GetNonEntityResourceType(Type type, IBsonSerializer serializer)
        {
            ResourceType resourceType;
            if (_knownResourceTypes.TryGetValue(type, out resourceType))
            {
                if (resourceType.ResourceTypeKind == ResourceTypeKind.EntityType)
                {
                    throw new InvalidOperationException("Entity types cannot be members of another entity type in MongoDB.");
                }

                return resourceType;
            }

            var arraySerializer = serializer as IBsonArraySerializer;
            if (arraySerializer != null)
            {
                var options = arraySerializer.GetItemSerializationInfo();
                var elementType = options.NominalType;
                var elementTypeSerializer = options.Serializer;
                var elementResourceType = GetNonEntityResourceType(elementType, elementTypeSerializer);
                return ResourceType.GetCollectionResourceType(elementResourceType);
            }

            var primitiveResourceType = ResourceType.GetPrimitiveResourceType(type);
            if (primitiveResourceType != null)
            {
                return primitiveResourceType;
            }

            var documentSerializer = serializer as IBsonDocumentSerializer;
            if (documentSerializer != null)
            {
                resourceType = BuildHierarchyForType(type, ResourceTypeKind.ComplexType);
                if (resourceType != null)
                {
                    return resourceType;
                }
            }

            throw new InvalidOperationException(string.Format("Unable to determine the resource type {0}.", type));
        }

        private void PopulateMetadataForTypes()
        {
            while (_unvisitedResourceTypes.Count > 0)
            {
                var tuple = _unvisitedResourceTypes.Dequeue();
                BuildResourceTypeProperties(tuple.Item1, tuple.Item2);
                BuildReflectionEpmInfo(tuple.Item2);

                var knownTypes = tuple.Item1.KnownTypes;
                foreach(var knownType in knownTypes)
                {
                    if (!_knownResourceTypes.ContainsKey(knownType))
                    {
                        BuildHierarchyForType(knownType, tuple.Item2.ResourceTypeKind);
                    }
                }
            }
        }

        private static TypedResourceSetAnnotation CreateResourceSetAnnotation(string globalDatabaseName, Type instanceType, Type dataContextType, PropertyInfo propertyInfo)
        {
            var databaseName = GetMongoDatabaseName(propertyInfo) ?? globalDatabaseName;
            var collectionName = GetMongoCollectionName(propertyInfo) ?? propertyInfo.Name;
            
            var dataContextParameter = Expression.Parameter(typeof(object));
            var getter = Expression.Lambda<Func<object, IQueryable>>(
                Expression.Call(
                    typeof(LinqExtensionMethods).GetMethod("AsQueryable", new[] { typeof(MongoCollection) }).MakeGenericMethod(instanceType),
                    Expression.Property(
                        Expression.Convert(dataContextParameter, dataContextType),
                        propertyInfo)),
                dataContextParameter).Compile();

            dataContextParameter = Expression.Parameter(typeof(object));
            var serverParameter = Expression.Parameter(typeof(MongoServer));

            var getDatabase = Expression.Call(
                serverParameter,
                "GetDatabase",
                Type.EmptyTypes,
                Expression.Constant(databaseName));

            var getCollection = Expression.Call(
                getDatabase,
                "GetCollection",
                new[] { instanceType },
                Expression.Constant(collectionName));

            var setter = Expression.Lambda<Action<object, MongoServer>>(
                Expression.Call(
                    Expression.Convert(dataContextParameter, dataContextType),
                    propertyInfo.GetSetMethod(true),
                    getCollection),
                dataContextParameter,
                serverParameter).Compile();

            return new TypedResourceSetAnnotation(databaseName, collectionName, getter, setter);
        }

        private static IEnumerable<PropertyInfo> GetCollectionProperties(Type dataContextType)
        {
            return dataContextType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead
                    && p.CanWrite
                    && p.GetIndexParameters().Length == 0
                    && p.PropertyType.IsGenericType
                    && p.PropertyType.GetGenericTypeDefinition() == typeof(MongoCollection<>));
        }

        private static Type GetMongoCollectionElementType(Type mongoCollectionType)
        {
            return mongoCollectionType.GetGenericArguments()[0];
        }

        private static string GetMongoDatabaseName(MemberInfo memberInfo)
        {
            var attribute = GetSingleAttribute<MongoDatabaseAttribute>(memberInfo);
            if (attribute != null)
            {
                return attribute.Name;
            }

            return null;
        }

        private static string GetMongoCollectionName(PropertyInfo propertyInfo)
        {
            var attribute = GetSingleAttribute<MongoCollectionAttribute>(propertyInfo);
            if (attribute != null)
            {
                return attribute.Name;
            }

            return null;
        }

        private static string GetResourceTypeName(Type type)
        {
            return type.Name;
        }

        private static T GetSingleAttribute<T>(MemberInfo member) where T : Attribute
        {
            return (T)member.GetCustomAttributes(typeof(T), false).SingleOrDefault();
        }

    }
}