using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Data.Services;
using System.Data.Services.Common;
using System.Data.Services.Providers;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.ServiceModel.Web;

namespace MongoDB.OData.Typed
{
    internal class TypedMetadataBuilder<T>
    {
        private readonly Dictionary<ResourceType, List<ResourceType>> _childResourceTypes;
        private readonly Dictionary<Type, ResourceType> _knownResourceTypes;
        private readonly Dictionary<string, ResourceSet> _resourceSets;
        private readonly Dictionary<string, ServiceOperation> _serviceOperations;
        private readonly Queue<ResourceType> _unvisitedResourceTypes;

        private string _containerNamespace;
        private string _containerName;

        public TypedMetadataBuilder()
        {
            _childResourceTypes = new Dictionary<ResourceType, List<ResourceType>>();
            _knownResourceTypes = new Dictionary<Type, ResourceType>();
            _resourceSets = new Dictionary<string, ResourceSet>();
            _serviceOperations = new Dictionary<string, ServiceOperation>();
            _unvisitedResourceTypes = new Queue<ResourceType>();
            BuildResourceSets();
            BuildServiceOperations();
            _containerNamespace = typeof(T).Namespace;
            _containerName = typeof(T).Name;
        }

        internal TypedMetadata BuildMetadata()
        {
            PopulateMetadataForTypes();

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

            var serviceOperations = _serviceOperations.Values.Select(x =>
            {
                x.SetReadOnly();
                return x;
            });

            return new TypedMetadata(_containerNamespace, _containerName, resourceSets, resourceTypes, serviceOperations);
        }

        private void AddResourceSet(string resourceSetName, Type documentType, TypedResourceSetAnnotation annotation)
        {
            if (_resourceSets.ContainsKey(resourceSetName))
            {
                throw new InvalidOperationException(string.Format("A resource set with the name {0} already exists.", resourceSetName));
            }

            var resourceType = BuildHierarchyForType(documentType, ResourceTypeKind.EntityType);
            if (resourceType == null)
            {
                throw new InvalidOperationException(string.Format("Type {0} is an invalid container.", documentType));
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

            var newResourceSet = new ResourceSet(resourceSetName, resourceType);
            newResourceSet.CustomState = annotation;
            _resourceSets.Add(newResourceSet.Name, newResourceSet);
        }

        private void AddServiceOperation(MethodInfo method, string protocol, object customState)
        {
            ServiceOperationResultKind resultKind;
            Type returnType;
            ResourceType returnResourceType;

            if (_serviceOperations.ContainsKey(method.Name))
            {
                throw new InvalidOperationException(string.Format("A service operation with the name {0} already exists.", method.Name));
            }

            if (method.ReturnType == typeof(void))
            {
                resultKind = ServiceOperationResultKind.Void;
                returnResourceType = null;
            }
            else
            {
                bool hasSingleResult = GetSingleAttribute<SingleResultAttribute>(method) != null;
                var queryableElementType = GetGenericElementType(method.ReturnType, typeof(IQueryable<>));
                if (queryableElementType == null)
                {
                    var enumerableElementType = GetGenericElementType(method.ReturnType, typeof(IEnumerable<>));
                    if (enumerableElementType == null)
                    {
                        returnType = method.ReturnType;
                        resultKind = ServiceOperationResultKind.DirectValue;
                    }
                    else
                    {
                        returnType = enumerableElementType;
                        resultKind = ServiceOperationResultKind.Enumeration;
                        if (hasSingleResult)
                        {
                            throw new InvalidOperationException(string.Format("Method {0} has a return type of IEnumerable which cannot have a SingleResultAttribute applied.", method.Name));
                        }
                    }
                }
                else
                {
                    returnType = queryableElementType;
                    if (hasSingleResult)
                    {
                        resultKind = ServiceOperationResultKind.QueryWithSingleResult;
                    }
                    else
                    {
                        resultKind = ServiceOperationResultKind.QueryWithMultipleResults;
                    }
                }
                returnResourceType = ResourceType.GetPrimitiveResourceType(returnType);
                if (returnResourceType == null)
                {
                    if (!_knownResourceTypes.TryGetValue(returnType, out returnResourceType))
                    {
                        returnResourceType = BuildHierarchyForType(returnType, ResourceTypeKind.ComplexType);
                    }
                }
            }

            var parameters = method.GetParameters();
            var serviceOperationParameters = new ServiceOperationParameter[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.IsOut || parameter.IsRetval)
                {
                    throw new InvalidOperationException(string.Format("Method {0} has Out or Retval parameters which cannot be used in ServiceOperations.", method.Name));
                }

                var parameterResourceType = ResourceType.GetPrimitiveResourceType(parameter.ParameterType);
                if (parameterResourceType == null)
                {
                    throw new InvalidOperationException(string.Format("Method {0} has a parameter of type {1} which cannot be used in a ServiceOperation.  Only primitive types are allowed as parameters.", method.Name, parameter.ParameterType.Name));
                }

                var parameterName = parameter.Name ?? "p" + i;
                serviceOperationParameters[i] = new ServiceOperationParameter(parameterName, parameterResourceType);
            }

            ResourceSet resourceSet = null;
            foreach (var set in _resourceSets.Values)
            {
                if (set.ResourceType.InstanceType.IsAssignableFrom(returnResourceType.InstanceType))
                {
                    resourceSet = set;
                    break;
                }
            }

            if (returnResourceType == null || returnResourceType.ResourceTypeKind != ResourceTypeKind.EntityType || resourceSet != null)
            {
                var serviceOperation = new ServiceOperation(method.Name, resultKind, returnResourceType, resourceSet, protocol, serviceOperationParameters);
                serviceOperation.CustomState = method;
                var mimeTypeAttribute = GetSingleAttribute<MimeTypeAttribute>(method);
                if (mimeTypeAttribute != null)
                {
                    serviceOperation.MimeType = mimeTypeAttribute.MimeType;
                }

                _serviceOperations.Add(serviceOperation.Name, serviceOperation);
            }
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
                entityResourceType = CreateResourceType(maps[i].ClassType, kind, entityResourceType);
            }

            PopulateMetadataForTypes();

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

        private void BuildResourceSets()
        {
            var dataContextType = typeof(T);
            var queryRootProperties = GetDataContextProperties(dataContextType);

            var globalDatabaseName = GetMongoDatabaseName(dataContextType) ?? dataContextType.Name;
            foreach (var queryRootProperty in queryRootProperties)
            {
                var documentType = GetMongoCollectionElementType(queryRootProperty.PropertyType);

                var annotation = CreateResourceSetAnnotation(globalDatabaseName, documentType, queryRootProperty);

                AddResourceSet(queryRootProperty.Name, documentType, annotation);
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

        private void BuildServiceOperations()
        {
            foreach (var method in typeof(T).GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (GetSingleAttribute<WebGetAttribute>(method) != null)
                {
                    AddServiceOperation(method, "GET", method);
                }
                else
                {
                    var webInvoke = GetSingleAttribute<WebInvokeAttribute>(method);
                    if (webInvoke != null)
                    {
                        AddServiceOperation(method, webInvoke.Method, method);
                    }
                }
            }
        }

        private ResourceType CreateResourceType(Type type, ResourceTypeKind kind, ResourceType baseType)
        {
            var resourceType = new ResourceType(type, kind, baseType, type.Namespace, GetResourceTypeName(type), type.IsAbstract);
            resourceType.IsOpenType = false;
            _knownResourceTypes.Add(type, resourceType);
            _childResourceTypes.Add(resourceType, null);
            if (baseType != null)
            {
                if (_childResourceTypes[baseType] == null)
                {
                    _childResourceTypes[baseType] = new List<ResourceType>();
                }
                _childResourceTypes[baseType].Add(resourceType);
            }

            _unvisitedResourceTypes.Enqueue(resourceType);
            return resourceType;
        }

        private ResourceType GetPropertyResourceType(BsonMemberMap memberMap)
        {
            var serializer = memberMap.GetSerializer();
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
                BsonSerializationInfo options;
                if (arraySerializer.TryGetItemSerializationInfo(out options))
                {
                    var elementType = options.NominalType;
                    var elementTypeSerializer = options.Serializer;
                    var elementResourceType = GetNonEntityResourceType(elementType, elementTypeSerializer);
                    return ResourceType.GetCollectionResourceType(elementResourceType);
                }
                return null;
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
                var resourceType = _unvisitedResourceTypes.Dequeue();
                var classMap = BsonClassMap.LookupClassMap(resourceType.InstanceType);
                resourceType.CustomState = new TypedResourceTypeAnnotation(classMap);
                BuildResourceTypeProperties(classMap, resourceType);
                BuildReflectionEpmInfo(resourceType);

                var knownTypes = classMap.KnownTypes;
                foreach(var knownType in knownTypes)
                {
                    if (!_knownResourceTypes.ContainsKey(knownType))
                    {
                        BuildHierarchyForType(knownType, resourceType.ResourceTypeKind);
                    }
                }
            }
        }

        private static TypedResourceSetAnnotation CreateResourceSetAnnotation(string globalDatabaseName, Type documentType, PropertyInfo propertyInfo)
        {
            var databaseName = GetMongoDatabaseName(propertyInfo) ?? globalDatabaseName;
            var collectionName = GetMongoCollectionName(propertyInfo) ?? propertyInfo.Name;

            var dataContextPropertyInfo = typeof(TypedDataSource).GetProperty("DataContext");
            var mongoServerPropertyInfo = typeof(TypedDataSource).GetProperty("Server");

            var dataSourceParameter = Expression.Parameter(typeof(TypedDataSource));
            var getQueryable = Expression.Lambda<Func<TypedDataSource, IQueryable>>(
                Expression.Call(
                    typeof(LinqExtensionMethods).GetMethod("AsQueryable", new[] { typeof(MongoCollection) }).MakeGenericMethod(documentType),
                    Expression.Property(
                        Expression.Convert(
                            Expression.Property(
                                dataSourceParameter,
                                dataContextPropertyInfo),
                            typeof(T)),
                        propertyInfo)),
                dataSourceParameter).Compile();

            dataSourceParameter = Expression.Parameter(typeof(TypedDataSource));

            var getDatabase = Expression.Call(
                Expression.Property(
                    dataSourceParameter,
                    mongoServerPropertyInfo),
                "GetDatabase",
                Type.EmptyTypes,
                Expression.Constant(databaseName));

            var getCollection = Expression.Call(
                getDatabase,
                "GetCollection",
                new[] { documentType },
                Expression.Constant(collectionName));

            var collectionGetter = Expression.Lambda<Func<TypedDataSource, MongoCollection>>(
                getCollection,
                dataSourceParameter).Compile();

            var setter = Expression.Lambda<Action<TypedDataSource>>(
                Expression.Call(
                    Expression.Convert(
                        Expression.Property(
                            dataSourceParameter,
                            dataContextPropertyInfo),
                        typeof(T)),
                    propertyInfo.GetSetMethod(true),
                    getCollection),
                dataSourceParameter).Compile();

            return new TypedResourceSetAnnotation(databaseName, collectionName, collectionGetter, getQueryable, setter);
        }

        private static IEnumerable<PropertyInfo> GetDataContextProperties(Type dataContextType)
        {
            return dataContextType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead
                    && p.CanWrite
                    && p.GetIndexParameters().Length == 0
                    && p.PropertyType.IsGenericType
                    && p.PropertyType.GetGenericTypeDefinition() == typeof(MongoCollection<>));
        }

        private static Type GetGenericElementType(Type enumerableType, Type genericTypeDefinition)
        {
            var @interface = enumerableType.GetInterfaces().SingleOrDefault(f => f.IsGenericType && f.GetGenericTypeDefinition() == genericTypeDefinition);
            if (@interface != null)
            {
                return @interface.GetGenericArguments()[0];
            }

            return null;
        }

        private static Type GetMongoCollectionElementType(Type mongoCollectionType)
        {
            return mongoCollectionType.GetGenericArguments()[0];
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

        private static string GetMongoDatabaseName(MemberInfo memberInfo)
        {
            var attribute = GetSingleAttribute<MongoDatabaseAttribute>(memberInfo);
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

        private static TAttribute GetSingleAttribute<TAttribute>(MemberInfo member) where TAttribute : Attribute
        {
            return (TAttribute)member.GetCustomAttributes(typeof(TAttribute), false).SingleOrDefault();
        }
    }
}