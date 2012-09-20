using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Reflection;

namespace MongoDB.OData.Typed
{
    internal class TypedQueryProvider : IDataServiceQueryProvider
    {
        private readonly TypedMetadata _metadata;

        public TypedQueryProvider(TypedMetadata metadata)
        {
            _metadata = metadata;
        }

        public object CurrentDataSource { get; set; }

        public bool IsNullPropagationRequired
        {
            get { return false; }
        }

        public object GetOpenPropertyValue(object target, string propertyName)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<KeyValuePair<string, object>> GetOpenPropertyValues(object target)
        {
            throw new NotSupportedException();
        }

        public object GetPropertyValue(object target, ResourceProperty resourceProperty)
        {
            throw new NotSupportedException();
        }

        public IQueryable GetQueryRootForResourceSet(ResourceSet resourceSet)
        {
            var annotation = (TypedResourceSetAnnotation)resourceSet.CustomState;
            return annotation.GetQueryableRoot((TypedDataSource)CurrentDataSource);
        }

        public ResourceType GetResourceType(object target)
        {
            return _metadata.Types.Single(x => x.InstanceType == target.GetType());
        }

        public object InvokeServiceOperation(ServiceOperation serviceOperation, object[] parameters)
        {
            var method = (MethodInfo)serviceOperation.CustomState;
            var currentDataContext = ((TypedDataSource)CurrentDataSource).DataContext;

            return method.Invoke(currentDataContext, parameters);
        }
    }
}