using System;
using System.Collections.Generic;
using System.Data.Services;
using System.Data.Services.Common;
using System.Linq;
using System.ServiceModel.Web;
using System.Web;
using MongoDB.Driver;
using MongoDB.OData.SampleModels.Blog;

namespace MongoDB.OData.SampleHost
{
    public class BlogApi : TypedMongoDataService
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
            builder.SetContainer("MongoDB.Samples", "BlogApi");
            builder.AddResourceSet<Blog>("Blogs", "odata_blogs", "blogs");
            builder.AddResourceSet<Post>("Posts", "odata_blogs", "posts");
            builder.AddResourceSet<User>("Users", "odata_blogs", "users");
        }

        protected override MongoServer CreateDataSource()
        {
            var server = MongoServer.Create();

            //create data if none exists...
            var db = server.GetDatabase("odata_blogs");
            var userCollection = db.GetCollection<User>("users");
            var blogCollection = db.GetCollection<BlogApi>("blogs");
            var postsCollection = db.GetCollection<Post>("posts");

            if (userCollection.Count() > 0)
                return server;

            var user1 = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Jack",
                LastName = "McJack",
                DisplayName = "jmcjack",
                LastLoginDateUtc = new DateTime(2012, 5,17, 21, 53,32).ToUniversalTime()
            };

            var user2 = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "McJane",
                DisplayName = "jmcjane",
                LastLoginDateUtc = new DateTime(2012, 5, 16, 3, 24, 31).ToUniversalTime()
            };

            userCollection.InsertBatch(new[] { user1, user2 });

            var blog = new Blog
            {
                Id = Guid.NewGuid(),
                Author = new UserSummary { Id = user1.Id, DisplayName = user1.DisplayName },
                Name = "Test Blog",
            };

            var post1 = new Post
            {
                Id = Guid.NewGuid(),
                BlogId = blog.Id,
                Title = "New OData Support",
                Abstract = "MongoDB supports OData!!!",
                Content = "MongoDB supports OData with the new MongoDB.OData provider",
                PostTimeUtc = new DateTime(2012, 5, 13, 23, 47, 58).ToUniversalTime(),
                Comments = new List<Comment>
                {
                    new Comment { Id = Guid.NewGuid(), Author = new UserSummary { Id = user2.Id, DisplayName = user2.DisplayName }, Content = "Love it!!!" }
                }
            };

            var post2 = new Post
            {
                Id = Guid.NewGuid(),
                BlogId = blog.Id,
                Title = "Updating with mongodb-dotnet-odata",
                Abstract = "Updating with mongodb-dotnet-odata",
                Content = "In order to update mongodb through odata, follow the below code samples.",
                PostTimeUtc = new DateTime(2012, 5, 17, 22, 12, 14).ToUniversalTime()
            };

            blog.Posts.Add(new BlogPostSummary { Id = post1.Id, Abstract = post1.Abstract, Title = post1.Title, PostTimeUtc = post1.PostTimeUtc });
            blog.Posts.Add(new BlogPostSummary { Id = post2.Id, Abstract = post2.Abstract, Title = post2.Title, PostTimeUtc = post2.PostTimeUtc });

            blogCollection.Insert(blog);
            postsCollection.InsertBatch(new[] { post1, post2 });

            return server;
        }
    }
}