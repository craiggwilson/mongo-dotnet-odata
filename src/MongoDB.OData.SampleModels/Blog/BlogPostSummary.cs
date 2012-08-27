using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MongoDB.OData.SampleModels.Blog
{
    public class BlogPostSummary
    {
        public Guid Id { get; set; }

        public string Title { get; set; }

        public string Abstract { get; set; }

        public DateTime PostTimeUtc { get; set; }
    }
}