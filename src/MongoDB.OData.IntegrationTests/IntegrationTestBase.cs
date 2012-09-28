using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Services;
using System.Data.Services.Client;
using System.Linq;
using System.ServiceModel.Web;
using System.Text;

namespace MongoDB.OData.IntegrationTests
{
    public abstract class IntegrationTestBase
    {
        private WebServiceHost _host;
        private DataServiceContext _context;

        protected DataServiceContext Context
        {
            get { return _context; }
        }

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            InitializeData();
            var uri = new Uri("http://localhost:1234/Blogs");
            var service = new TestService();
            _host = new WebServiceHost(service, new[] { uri });
            _host.Open();
            _context = new TestDataContext(uri);
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            _host.Close();
        }

        protected void InitializeData()
        {
            var server = MongoServer.Create();
            var db = server.GetDatabase("odata_tests");
            InitializePeople(db);
        }

        private static void InitializePeople(MongoDatabase db)
        {
            db.DropCollection("people");
            var people = db.GetCollection<Person>("people");

            var peopleToSave = new List<Person>();
            peopleToSave.Add(new Person
            {
                Name = new Name { First = "Jack", Last = "McJack" },
                BirthDate = DateTime.Parse("1/1/1984")
            });

            peopleToSave.Add(new Person
            {
                Name = new Name { First = "Bob", Last = "McBob" },
                BirthDate = DateTime.Parse("12/1/1956")
            });

            people.InsertBatch(peopleToSave);
        }

        private class TestDataContext : DataServiceContext
        {
            public TestDataContext(Uri serviceRoot)
                : base(serviceRoot, System.Data.Services.Common.DataServiceProtocolVersion.V3)
            {
                base.ResolveEntitySet = ResolveEntitySet;
                base.ResolveName = ResolveNameFromType;
                base.ResolveType = ResolveTypeFromName;
            }

            protected Type ResolveTypeFromName(string typeName)
            {

                if (typeName.StartsWith("TestService."))
                {
                    return this.GetType().Assembly.GetType(
                       typeName.Replace("TestService.", "MongoDB.OData.IntegrationTests."),
                       false);
                }

                return null;
            }

            protected string ResolveNameFromType(Type clientType)
            {
                if (clientType.Namespace.Equals("MongoDB.OData.IntegrationTests"))
                {
                    return "TestService." + clientType.Name;
                }

                return null;
            }
        }
    }
}