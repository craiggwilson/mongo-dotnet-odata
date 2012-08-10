using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MongoDB.OData.SampleModels
{
    public class UserSummary
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; }
    }
}