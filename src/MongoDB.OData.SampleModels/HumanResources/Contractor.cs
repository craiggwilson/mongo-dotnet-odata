using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.OData.SampleModels.HumanResources
{
    public class Contractor : Employee
    {
        public string Address { get; set; }
    }
}
