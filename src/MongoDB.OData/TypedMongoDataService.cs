using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Services;
using MongoDB.Driver;
using System.Data.Services.Providers;

namespace MongoDB.OData
{
    public abstract class TypedMongoDataService : DataService<MongoServer>, IServiceProvider
    {
        private static object _metadataLock = new object();
        private static TypedMongoDataServiceMetadataProvider _metadataProvider;

        protected static void Configure(DataServiceConfiguration config)
        {
            config.DataServiceBehavior.AcceptProjectionRequests = false;
            config.DataServiceBehavior.AcceptSpatialLiteralsInQuery = false;
            config.DataServiceBehavior.MaxProtocolVersion = System.Data.Services.Common.DataServiceProtocolVersion.V3;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IDataServiceMetadataProvider))
            {
                return GetMetadataProvider();
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

        private TypedMongoDataServiceMetadataProvider GetMetadataProvider()
        {
            //if (_metadataProvider == null)
            //{
            //    lock (_metadataLock)
            //    {
            //        if (_metadataProvider == null)
            //        {
                        var metadata = new TypedMongoDataServiceMetadata();
                        BuildMetadata(metadata);
                        _metadataProvider = metadata.CreateMetadataProvider();
            //        }
            //    }
            //}

            return _metadataProvider;
        }

        private TypedMongoDataServiceQueryProvider GetQueryProvider()
        {
            return new TypedMongoDataServiceQueryProvider(GetMetadataProvider());
        }

        protected abstract void BuildMetadata(TypedMongoDataServiceMetadata metadata);

        protected abstract override MongoServer CreateDataSource();
    }
}