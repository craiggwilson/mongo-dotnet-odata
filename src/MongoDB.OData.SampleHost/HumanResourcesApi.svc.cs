using System;
using System.Collections.Generic;
using System.Data.Services;
using System.Data.Services.Common;
using System.Linq;
using System.ServiceModel.Web;
using System.Web;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.OData.SampleModels.HumanResources;

namespace MongoDB.OData.SampleHost
{
    public class HumanResourcesApi : TypedMongoDataService
    {
        // This method is called only once to initialize service-wide policies.
        public static void InitializeService(DataServiceConfiguration config)
        {
            TypedMongoDataService.Configure(config);
            config.SetEntitySetAccessRule("*", EntitySetRights.All);
            config.UseVerboseErrors = true;
        }

        protected override void BuildMetadata(TypedMongoDataServiceMetadataBuilder builder)
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(Person)))
            {
                BsonClassMap.RegisterClassMap<Person>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIsRootClass(true);
                    cm.AddKnownType(typeof(Employee));
                    cm.AddKnownType(typeof(Manager));
                    cm.AddKnownType(typeof(Contractor));
                });
            }

            builder.SetContainer("MongoDB.Samples", "HumanResources");
            builder.AddResourceSet<Person>("People", "odata_hr", "people");
        }

        protected override MongoServer CreateDataSource()
        {
            var server = MongoServer.Create();

            //create data if none exists...
            var db = server.GetDatabase("odata_hr");

            var people = db.GetCollection<Person>("people");

            if (people.Count() > 0)
            {
                return server;
            }

            var employee1 = new Employee
            {
                Id = "Employee1",
                HireDate = new DateTime(2005, 1, 1),
                Name = new Name { First = "Jack", Last = "McJack" },
                Salary = 1200
            };

            var employee2 = new Employee
            {
                Id = "Employee2",
                HireDate = new DateTime(2007, 1, 1),
                Name = new Name { First = "Jane", Last = "McJane" },
                Salary = 1300
            };

            var contractor1 = new Contractor
            {
                Id = "Contractor1",
                HireDate = new DateTime(2010, 1, 1),
                Name = new Name { First = "Joe", Last = "McJoe" },
                Salary = 300,
                Address = "123 Main ST.",
            };

            var manager1 = new Manager
            {
                Id = "Manager1",
                HireDate = new DateTime(2000, 1, 1),
                Name = new Name { First = "Jim", Last = "McJim" },
                Salary = 2000,
                Employees = new List<PersonRef>
                {
                    new PersonRef { Id = employee1.Id, Name = employee1.Name },
                    new PersonRef { Id = employee2.Id, Name = employee2.Name },
                    new PersonRef { Id = contractor1.Id, Name = contractor1.Name },
                }
            };

            people.Save(employee1);
            people.Save(employee2);
            people.Save(contractor1);
            people.Save(manager1);

            return server;
        }
    }
}