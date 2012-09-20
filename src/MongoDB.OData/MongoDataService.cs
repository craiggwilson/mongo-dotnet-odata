using MongoDB.Driver;
using MongoDB.OData.Typed;
using System;
using System.Data.Services;
using System.Data.Services.Common;
using System.Data.Services.Providers;

namespace MongoDB.OData
{
    /// <summary>
    /// Base class for creating a WCF Data Service to connect to MongoDB.
    /// </summary>
    /// <typeparam name="TDataContext">The type of the data context.</typeparam>
    public abstract class MongoDataService<TDataContext> : DataService<TypedDataSource>, IServiceProvider
    {
        private static object _metadataLock = new object();
        private static TypedMetadata _metadata;

        /// <summary>
        /// Some basic configuration that should be used.
        /// </summary>
        /// <param name="config">The config.</param>
        protected static void Configure(DataServiceConfiguration config)
        {
            config.DataServiceBehavior.AcceptProjectionRequests = false;
            config.DataServiceBehavior.AcceptSpatialLiteralsInQuery = false;
            config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V3;
        }

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <returns>
        /// A service object of type <paramref name="serviceType" />.-or- null if there is no service object of type <paramref name="serviceType" />.
        /// </returns>
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
            else if (serviceType == typeof(IDataServiceUpdateProvider))
            {
                return GetUpdateProvider();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Creates the data source.
        /// </summary>
        /// <returns></returns>
        protected sealed override TypedDataSource CreateDataSource()
        {
            var server = CreateMongoServer();
            var dataContext = CreateDataContext(server);
            var dataSource = new TypedDataSource(server, dataContext);
            InitializeDataContext(dataSource);
            return dataSource;
        }

        /// <summary>
        /// Creates the data context.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <returns>An instance of the data context.</returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        protected virtual TDataContext CreateDataContext(MongoServer server)
        {
            var ctor = typeof(TDataContext).GetConstructor(new[] { typeof(MongoServer) });

            if (ctor != null)
            {
                return (TDataContext)ctor.Invoke(new[] { server });
            }

            ctor = typeof(TDataContext).GetConstructor(Type.EmptyTypes);

            if (ctor != null)
            {
                return (TDataContext)ctor.Invoke(new object[0]);
            }

            throw new InvalidOperationException(string.Format("Either overload the CreateDataContext(MongoServer) method or ensure that {0} has an empty ctor or a ctor that take a single MongoServer parameter.", typeof(TDataContext)));
        }

        /// <summary>
        /// Creates the mongo server.
        /// </summary>
        /// <returns>An instance of the MongoServer used for connecting to the mongodb servers.</returns>
        protected abstract MongoServer CreateMongoServer();

        private TypedMetadata GetMetadata()
        {
            if (_metadata == null)
            {
                lock (_metadataLock)
                {
                    if (_metadata == null)
                    {
                        var builder = new TypedMetadataBuilder<TDataContext>();
                        _metadata = builder.BuildMetadata();
                    }
                }
            }

            return _metadata;
        }

        private TypedQueryProvider GetQueryProvider()
        {
            return new TypedQueryProvider(GetMetadata());
        }

        private TypedUpdateProvider GetUpdateProvider()
        {
            return new TypedUpdateProvider(CurrentDataSource, GetMetadata());
        }

        private void InitializeDataContext(TypedDataSource dataSource)
        {
            var metadata = GetMetadata();
            foreach (var resourceSet in metadata.ResourceSets)
            {
                var annotation = (TypedResourceSetAnnotation)resourceSet.CustomState;
                annotation.SetDataContext(dataSource);
            }
        }
    }
}