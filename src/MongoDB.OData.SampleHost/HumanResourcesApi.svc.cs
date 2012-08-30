using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.OData.SampleModels.HumanResources;
using System;
using System.Collections.Generic;
using System.Data.Services;
using System.Linq;
using System.ServiceModel.Web;

namespace MongoDB.OData.SampleHost
{
    [MongoDatabase("odata_hr")]
    public class HumanResourceEntities
    {
        [MongoCollection("people")]
        public MongoCollection<Person> People { get; set; }

        [WebGet]
        public IQueryable<Employee> GetEmployeesOfManager(string managerId)
        {
            var manager = People.AsQueryable().OfType<Manager>().SingleOrDefault(m => m.Id == managerId);
            if (manager == null)
            {
                throw new DataServiceException(404, string.Format("Manager with id {0} does not exist.", managerId));
            }

            var employeeIds = manager.Employees.Select(m => m.Id);
            return People.AsQueryable().OfType<Employee>().Where(e => employeeIds.Contains(e.Id));
        }
    }

    public class HumanResourcesApi : MongoDataService<HumanResourceEntities>
    {
        static HumanResourcesApi()
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
        }

        // This method is called only once to initialize service-wide policies.
        public static void InitializeService(DataServiceConfiguration config)
        {
            Configure(config);
            config.SetEntitySetAccessRule("*", EntitySetRights.All);
            config.SetServiceOperationAccessRule("*", ServiceOperationRights.AllRead);
            config.UseVerboseErrors = true;
        }

        protected override MongoServer CreateMongoServer()
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