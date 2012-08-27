using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.OData.SampleModels.HumanResources
{
    public class Manager : Employee
    {
        public List<PersonRef> Employees { get; set; }
    }
}
