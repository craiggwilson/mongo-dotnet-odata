using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MongoDB.OData.SampleModels.Blog
{
    public class Comment
    {
        public Guid Id { get; set; }

        public UserSummary Author { get; set; }

        public string Content { get; set; }
    }
}