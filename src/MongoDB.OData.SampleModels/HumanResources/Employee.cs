using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.OData.SampleModels.HumanResources
{
    public class Employee : Person
    {
        public DateTime HireDate { get; set; }

        public int Salary { get; set; }
    }
}
