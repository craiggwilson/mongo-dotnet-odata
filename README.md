mongo-dotnet-odata
==================

An OData Provider for MongoDB. Requires OData version 3 for support of mongodb arrays.

Getting Started
---------------

It's extremely easy to get an odata service up and running with mongodb.  Below are a few simple steps:

1. Using nuget, simply pull down the [MongoDB.OData](http://www.nuget.org/packages/MongoDB.OData) package.  
2. Create a class that you want to be your OData container name.
	
	    [MongoDatabase("hr")]
		public class Sample
		{
			[MongoCollection("people")]
			public MongoCollection<Person> People { get; set; }

			[MongoCollection("users")]
			public MongoCollection<User> Users { get; set; }
		}

3. Create a WCF Data Service
4. Inherit from `MongoDataService<T>` and implement the abstract methods.  Also, like any other WCF data service, you need to configure the entity.

		public class SampleService : MongoDataService<Sample>
		{
		    // This method is called only once to initialize service-wide policies.
	        public static void InitializeService(DataServiceConfiguration config)
	        {
	        	// MongoDataService<T> has a method that pre-configures some stuff for you...
	            Configure(config); 

	            // Set these as necessary
	            config.SetEntitySetAccessRule("*", EntitySetRights.All);
	            config.SetServiceOperationAccessRule("*", ServiceOperationRights.AllRead);
	        }

	        protected override MongoServer CreateMongoServer()
	        {
	        	// use whatever your connection string is.  if you don't know how to form a 
	        	// mongodb connection string, please refer to the mongodb documentation at 
	        	// http://www.mongodb.org/display/DOCS/CSharp+Driver+Tutorial#CSharpDriverTutorial-Connectionstrings.
	        	return MongoServer.Create();
	    	}
		}

5. That's it.  You can see the [samples](https://github.com/craiggwilson/mongo-dotnet-odata/tree/master/src/MongoDB.OData.SampleHost) for working examples.

Supported Features
------------------

- Queries
  - OData Entity Types - MongoDB Documents
  - OData Complex Types - MongoDB Embedded Documents
  - OData Collections - MongoDB Arrays
- OData Service Operations

Future Development
------------------
- Projections
- Spatial Literals
- Updating
- ObjectId translation