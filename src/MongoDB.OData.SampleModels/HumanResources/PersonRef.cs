using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.OData.SampleModels.HumanResources
{
    public class PersonRef
    {
        public string Id { get; set; }

        public Name Name { get; set; }
    }
}
