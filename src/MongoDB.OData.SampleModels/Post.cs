using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MongoDB.OData.SampleModels
{
    public class Post
    {
        public Guid Id { get; set; }

        public Guid BlogId { get; set; }

        public string Title { get; set; }

        public string Abstract { get; set; }

        public string Content { get; set; }

        public DateTime PostTimeUtc { get; set; }

        public List<Comment> Comments { get; set; }

        public Post()
        {
            Comments = new List<Comment>();
        }
    }
}