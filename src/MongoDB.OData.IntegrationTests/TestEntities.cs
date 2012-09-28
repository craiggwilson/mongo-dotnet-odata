using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data.Services;
using System.Data.Services.Client;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace MongoDB.OData.IntegrationTests
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true, InstanceContextMode = InstanceContextMode.Single)]
    public class TestService : MongoDataService<TestEntities>
    {
        public static void InitializeService(DataServiceConfiguration config)
        {
            Configure(config);
            config.SetEntitySetAccessRule("*", EntitySetRights.All);
            config.SetServiceOperationAccessRule("*", ServiceOperationRights.AllRead);
            config.UseVerboseErrors = true;
        }

        protected override MongoServer CreateMongoServer()
        {
            return MongoServer.Create();
        }
    }

    [MongoDatabase("odata_tests")]
    public class TestEntities
    {
        [MongoCollection("people")]
        public MongoCollection<Person> People { get; set; }
    }

    public class Person
    {
        public Guid Id { get; set; }

        [BsonElement("name")]
        public Name Name { get; set; }

        [BsonElement("birthdate")]
        public DateTime BirthDate { get; set; }
    }

    public class Name
    {
        [BsonElement("first")]
        public string First { get; set; }

        [BsonElement("last")]
        public string Last { get; set; }
    }
}