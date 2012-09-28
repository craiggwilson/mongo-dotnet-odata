using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Services.Client;

namespace MongoDB.OData.IntegrationTests
{
    public class When_querying : IntegrationTestBase
    {
        [Test]
        public void should_find_all()
        {
            var query = Context.CreateQuery<Person>("People");

            var people = query.ToList();

            people.Count.Should().Be(2);
        }
    }
}