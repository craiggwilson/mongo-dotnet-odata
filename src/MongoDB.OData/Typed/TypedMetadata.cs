using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;

namespace MongoDB.OData.Typed
{
    internal class TypedMetadata : IDataServiceMetadataProvider
    {
        private readonly Dictionary<string, ResourceSet> _sets;
        private readonly Dictionary<string, ResourceType> _types;
        private readonly Dictionary<string, ResourceType> _qualifiedTypes;
        private readonly Dictionary<ResourceType, List<ResourceType>> _derivedTypes;

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

        public TypedMetadata(string containerNamespace, string containerName, IEnumerable<ResourceSet> resourceSets, IEnumerable<ResourceType> resourceTypes)
        {
            ContainerNamespace = containerNamespace ?? "MongoDB";
            ContainerName = containerName ?? "Database";

            _sets = resourceSets.ToDictionary(x => x.Name, x => x);
            _types = resourceTypes.ToDictionary(x => x.Name, x => x);
            _qualifiedTypes = resourceTypes.ToDictionary(x => x.FullName, x => x);
            _derivedTypes = new Dictionary<ResourceType, List<ResourceType>>();

            foreach (var type in resourceTypes.Where(t => t.BaseType != null))
            {
                List<ResourceType> derivedTypes;
                if (!_derivedTypes.TryGetValue(type.BaseType, out derivedTypes))
                {
                    _derivedTypes[type.BaseType] = derivedTypes = new List<ResourceType>();
                }

                derivedTypes.Add(type);
            }
        }

        public IEnumerable<ResourceType> GetDerivedTypes(ResourceType resourceType)
        {
            List<ResourceType> derivedTypes;
            if (!_derivedTypes.TryGetValue(resourceType, out derivedTypes))
            {
                return Enumerable.Empty<ResourceType>();
            }

            List<ResourceType> result = new List<ResourceType>(derivedTypes);

            foreach (var derivedType in derivedTypes)
            {
                result.AddRange(GetDerivedTypes(derivedType));
            }

            return result;
        }

        public ResourceAssociationSet GetResourceAssociationSet(ResourceSet resourceSet, ResourceType resourceType, ResourceProperty resourceProperty)
        {
            throw new NotImplementedException();
        }

        public bool HasDerivedTypes(ResourceType resourceType)
        {
            List<ResourceType> derivedTypes;
            if (_derivedTypes.TryGetValue(resourceType, out derivedTypes))
            {
                return derivedTypes != null && derivedTypes.Count > 0;
            }

            return false;
        }

        public bool TryResolveResourceSet(string name, out ResourceSet resourceSet)
        {
            return _sets.TryGetValue(name, out resourceSet);
        }

        public bool TryResolveResourceType(string name, out ResourceType resourceType)
        {
            return _types.TryGetValue(name, out resourceType) || _qualifiedTypes.TryGetValue(name, out resourceType);
        }

        public bool TryResolveServiceOperation(string name, out ServiceOperation serviceOperation)
        {
            serviceOperation = null;
            return false;
        }
    }
}