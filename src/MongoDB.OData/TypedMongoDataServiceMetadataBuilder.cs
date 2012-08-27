using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Services.Providers;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Bson;

namespace MongoDB.OData
{
    public class TypedMongoDataServiceMetadataBuilder
    {
        private readonly Dictionary<string, ResourceSet> _sets;
        private readonly Dictionary<string, ResourceType> _types;
        private string _containerNamespace;
        private string _containerName;
        private TypedMongoResourceSetAnnotation _currentSetAnnotation;
        private bool _isAtRoot;

        internal TypedMongoDataServiceMetadataBuilder()
        {
            _sets = new Dictionary<string, ResourceSet>();
            _types = new Dictionary<string, ResourceType>();
        }

        public void SetContainer(string containerNamespace, string containerName)
        {
            _containerNamespace = containerNamespace;
            _containerName = containerName;
        }

        public void AddResourceSet<TClass>(string resourceSetName, string databaseName, string collectionName)
        {
            AddResourceSet(typeof(TClass), resourceSetName, databaseName, collectionName);
        }

        public void AddResourceSet(Type type, string resourceSetName, string databaseName, string collectionName)
        {
            _currentSetAnnotation = new TypedMongoResourceSetAnnotation(databaseName, collectionName);
            _isAtRoot = true;
            var serializer = BsonSerializer.LookupSerializer(type);
            var resourceType = GetOrAddResourceType(type, serializer, ResourceTypeKind.EntityType);
            var resourceSet = new ResourceSet(resourceSetName, resourceType);
            resourceSet.CustomState = _currentSetAnnotation;
            _sets.Add(resourceSetName, resourceSet);
            _currentSetAnnotation = null;
        }

        internal TypedMongoDataServiceMetadataProvider CreateMetadataProvider()
        {
            CreateTypeDataAnnotations();

            foreach (var set in _sets)
            {
                set.Value.SetReadOnly();
            }

            foreach (var type in _types)
            {
                type.Value.SetReadOnly();
            }

            return new TypedMongoDataServiceMetadataProvider(
                _containerNamespace,
                _containerName,
                _sets,
                _types);
        }

        private ResourceProperty CreateResourceProperty(BsonMemberMap memberMap, ResourceTypeKind typeKind)
        {
            var saveIsAtRoot = _isAtRoot;
            _isAtRoot = true;
            var serializer = memberMap.GetSerializer(memberMap.MemberType);
            var resourceType = GetOrAddResourceType(memberMap.MemberType, serializer);
            _isAtRoot = saveIsAtRoot;

            ResourcePropertyKind kind;
            switch (resourceType.ResourceTypeKind)
            {
                case ResourceTypeKind.Collection:
                    kind = ResourcePropertyKind.Collection;
                    break;
                case ResourceTypeKind.ComplexType:
                    kind = ResourcePropertyKind.ComplexType;
                    break;
                case ResourceTypeKind.Primitive:
                    kind = ResourcePropertyKind.Primitive;
                    break;
                default:
                    throw new NotSupportedException();
            }

            //complex types can't have key properties
            if (typeKind == ResourceTypeKind.EntityType && memberMap == memberMap.ClassMap.IdMemberMap)
            {
                kind |= ResourcePropertyKind.Key;
            }

            var resourceProperty = new ResourceProperty(
                memberMap.MemberName,
                kind,
                resourceType);

            resourceProperty.CustomState = new TypedMongoResourcePropertyAnnotation(memberMap);
            return resourceProperty;
        }

        private void CreateTypeDataAnnotations()
        {
            foreach (var resourceType in _types.Values)
            {
                var annotation = resourceType.CustomState as TypedMongoResourceTypeAnnotation;
                if (annotation == null)
                {
                    continue;
                }

                var derivedTypes = _types.Values.Where(x => x.BaseType == resourceType);
                foreach (var derivedType in derivedTypes)
                {
                    annotation.AddDerivedTyped(derivedType);
                }
            }
        }

        private ResourceType GetOrAddResourceType(Type type, IBsonSerializer serializer)
        {
            ResourceType resourceType;
            if (!_types.TryGetValue(GetResourceTypeNameForType(type), out resourceType))
            {
                ResourceTypeKind kind = ResourceTypeKind.Primitive;
                if (IsCollectionResourceType(type))
                {
                    kind = ResourceTypeKind.Collection;
                }
                else if (IsComplexResourceType(type))
                {
                    kind = ResourceTypeKind.ComplexType;
                }

                return GetOrAddResourceType(type, serializer, kind);
            }

            return resourceType;
        }

        private ResourceType GetOrAddResourceType(Type type, IBsonSerializer serializer, ResourceTypeKind kind)
        {
            ResourceType resourceType;
            if (!_types.TryGetValue(GetResourceTypeNameForType(type), out resourceType))
            {
                switch (kind)
                {
                    case ResourceTypeKind.EntityType:
                    case ResourceTypeKind.ComplexType:
                        resourceType = GetOrAddEntityOrComplexResourceType(type, serializer, kind);
                        break;
                    case ResourceTypeKind.Collection:
                        var itemSerializationInfo = (serializer as IBsonArraySerializer).GetItemSerializationInfo();
                        var resourceItemType = GetOrAddResourceType(itemSerializationInfo.NominalType, itemSerializationInfo.Serializer);
                        resourceType = ResourceType.GetCollectionResourceType(resourceItemType);
                        break;
                    case ResourceTypeKind.Primitive:
                        resourceType = ResourceType.GetPrimitiveResourceType(type);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }

            return resourceType;
        }

        private ResourceType GetOrAddEntityOrComplexResourceType(Type type, IBsonSerializer serializer, ResourceTypeKind kind)
        {
            //we are expecting that this is a classmap this point.
            var classMap = BsonClassMap.LookupClassMap(type);

            ResourceType baseResourceType = null;
            if ((!_isAtRoot || kind == ResourceTypeKind.ComplexType) && classMap.BaseClassMap != null)
            {
                var baseSerializer = BsonSerializer.LookupSerializer(classMap.BaseClassMap.ClassType);
                //this should have already been mapped...
                baseResourceType = GetOrAddResourceType(classMap.BaseClassMap.ClassType, baseSerializer, kind);
            }

            //we may have already mapped this type based on inheritance structure
            if (_types.ContainsKey(GetResourceTypeNameForType(type)))
            {
                return _types[GetResourceTypeNameForType(type)];
            }

            var resourceType = new ResourceType(
                type,
                kind,
                baseResourceType,
                type.Namespace,
                GetResourceTypeNameForType(type),
                type.IsAbstract);

            resourceType.CustomState = new TypedMongoResourceTypeAnnotation(_currentSetAnnotation, classMap);

            var memberMaps = baseResourceType == null
                ? classMap.AllMemberMaps //get all the ones on me and my parents...
                : classMap.DeclaredMemberMaps; //just get me because the others will come from the base resource type...

            foreach (var memberMap in memberMaps)
            {
                resourceType.AddProperty(CreateResourceProperty(memberMap, kind));
            }

            // only add the type if it contains at least 1 property per odata rules...
            if (resourceType.Properties.Count > 0)
            {
                _types.Add(GetResourceTypeNameForType(type), resourceType);
            }
            else
            {
                resourceType = null;
            }

            bool saveIsAtRoot = _isAtRoot;
            _isAtRoot = false;
            foreach (var knownType in classMap.KnownTypes)
            {
                var knownTypeSerializer = BsonSerializer.LookupSerializer(knownType);
                GetOrAddResourceType(knownType, knownTypeSerializer, kind);
            }
            _isAtRoot = saveIsAtRoot;
            return resourceType;
        }

        private static string GetResourceTypeNameForType(Type type)
        {
            return type.Name;
        }

        private static bool IsCollectionResourceType(Type type)
        {
            var serializer = BsonSerializer.LookupSerializer(type);
            return serializer is IBsonArraySerializer;
        }

        private static bool IsComplexResourceType(Type type)
        {
            var serializer = BsonSerializer.LookupSerializer(type);
            var resourceType = ResourceType.GetPrimitiveResourceType(type);
            return resourceType == null && serializer is IBsonDocumentSerializer;
        }
    }
}