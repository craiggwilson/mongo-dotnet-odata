using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MongoDB.OData.SampleModels
{
    public class User
    {
        public Guid Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string DisplayName { get; set; }

        public DateTime LastLoginDateUtc { get; set; }
    }
}