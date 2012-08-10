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
    internal class TypedMongoDataServiceMetadataProvider : IDataServiceMetadataProvider
    {
        private readonly Dictionary<string, ResourceSet> _sets;
        private readonly Dictionary<string, ResourceType> _types;

        public string ContainerName { get; private set; }

        public string ContainerNamespace { get; private set; }

        public IEnumerable<ResourceSet> ResourceSets
        {
            get { return _sets.Values; }
        }

        public IEnumerable<ServiceOperation> ServiceOperations
        {
            get { yield break; }
        }

        public IEnumerable<ResourceType> Types
        {
            get { return _types.Values; }
        }

        public TypedMongoDataServiceMetadataProvider(string containerNamespace, string containerName, Dictionary<string, ResourceSet> resourceSets, Dictionary<string, ResourceType> resourceTypes)
        {
            ContainerNamespace = containerNamespace ?? "MongoDB";
            ContainerName = containerName ?? "Database";

            _sets = resourceSets;
            _types = resourceTypes;
        }

        public IEnumerable<ResourceType> GetDerivedTypes(ResourceType resourceType)
        {
            var annotation = resourceType.CustomState as TypedMongoResourceTypeAnnotation;
            if (annotation == null)
                return Enumerable.Empty<ResourceType>();

            return annotation.DerivedTypes;
        }

        public ResourceAssociationSet GetResourceAssociationSet(ResourceSet resourceSet, ResourceType resourceType, ResourceProperty resourceProperty)
        {
            throw new NotImplementedException();
        }

        public bool HasDerivedTypes(ResourceType resourceType)
        {
            var annotation = resourceType.CustomState as TypedMongoResourceTypeAnnotation;
            if (annotation == null)
                return false;

            return annotation.HasDerivedTypes;
        }

        public bool TryResolveResourceSet(string name, out ResourceSet resourceSet)
        {
            return _sets.TryGetValue(name, out resourceSet);
        }

        public bool TryResolveResourceType(string name, out ResourceType resourceType)
        {
            return _types.TryGetValue(name, out resourceType);
        }

        public bool TryResolveServiceOperation(string name, out ServiceOperation serviceOperation)
        {
            serviceOperation = null;
            return false;
        }
    }
}