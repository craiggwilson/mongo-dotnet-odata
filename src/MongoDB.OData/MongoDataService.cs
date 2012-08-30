using MongoDB.Driver;
using MongoDB.OData.Typed;
using System;
using System.Data.Services;
using System.Data.Services.Common;
using System.Data.Services.Providers;

namespace MongoDB.OData
{
    public abstract class MongoDataService<T> : DataService<TypedDataSource>, IServiceProvider
    {
        private static object _metadataLock = new object();
        private static TypedMetadata _metadata;

        protected static void Configure(DataServiceConfiguration config)
        {
            config.DataServiceBehavior.AcceptProjectionRequests = false;
            config.DataServiceBehavior.AcceptSpatialLiteralsInQuery = false;
            config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V3;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IDataServiceMetadataProvider))
            {
                return GetMetadata();
            }
            else if (serviceType == typeof(IDataServiceQueryProvider))
            {
                return GetQueryProvider();
            }
            else
            {
                return null;
            }
        }

        protected sealed override TypedDataSource CreateDataSource()
        {
            var server = CreateMongoServer();
            var dataContext = CreateDataContext(server);
            var dataSource = new TypedDataSource(server, dataContext);
            InitializeDataContext(dataSource);
            return dataSource;
        }

        protected virtual T CreateDataContext(MongoServer server)
        {
            var ctor = typeof(T).GetConstructor(new[] { typeof(MongoServer) });

            if (ctor != null)
            {
                return (T)ctor.Invoke(new[] { server });
            }

            ctor = typeof(T).GetConstructor(Type.EmptyTypes);

            if (ctor != null)
            {
                return (T)ctor.Invoke(new object[0]);
            }

            throw new InvalidOperationException(string.Format("Either overload the CreateDataContext(MongoServer) method or ensure that {0} has an empty ctor or a ctor that take a single MongoServer parameter.", typeof(T)));
        }

        protected abstract MongoServer CreateMongoServer();

        private TypedMetadata GetMetadata()
        {
            //if (_metadataProvider == null)
            //{
            //    lock (_metadataLock)
            //    {
            //        if (_metadataProvider == null)
            //        {
            var builder = new TypedMetadataBuilder<T>();
            _metadata = builder.BuildMetadata();
            //        }
            //    }
            //}

            return _metadata;
        }

        private TypedQueryProvider GetQueryProvider()
        {
            return new TypedQueryProvider(GetMetadata());
        }

        private void InitializeDataContext(TypedDataSource dataSource)
        {
            var metadata = GetMetadata();
            foreach(var resourceSet in metadata.ResourceSets)
            {
                var annotation = (TypedResourceSetAnnotation)resourceSet.CustomState;
                annotation.SetDataContext(dataSource);
            }
        }
    }
}