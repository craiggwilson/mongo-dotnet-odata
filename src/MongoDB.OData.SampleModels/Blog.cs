using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MongoDB.OData.SampleModels
{
    public class Blog
    {
        public Guid Id { get; set; }

        public UserSummary Author { get; set; }

        public string Name { get; set; }

        public List<BlogPostSummary> Posts { get; set; }

        public Blog()
        {
            Posts = new List<BlogPostSummary>();
        }
    }
}