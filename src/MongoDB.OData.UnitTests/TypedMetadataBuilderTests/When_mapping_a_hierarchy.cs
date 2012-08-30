using FluentAssertions;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.OData.Typed;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;

namespace MongoDB.OData.UnitTests
{
    [TestFixture]
    internal class When_mapping_a_hierarchy : Specification<TypedMetadata>
    {
        protected override TypedMetadata EstablishContext()
        {
            return new TypedMetadataBuilder<Hierarchy>().BuildMetadata();
        }

        [Test]
        public void should_add_only_1_resource_set()
        {
            Subject.ResourceSets.Count().Should().Be(1);
        }

        [Test]
        public void should_map_the_resource_set()
        {
            var set = Subject.ResourceSets.Single();
            set.ResourceType.InstanceType.Should().Be(typeof(Person));
            set.ResourceType.IsAbstract.Should().BeTrue();
        }

        [Test]
        public void should_recognize_the_derived_types()
        {
            var personType = Subject.Types.Single(x => x.InstanceType == typeof(Person));
            Subject.HasDerivedTypes(personType).Should().BeTrue();
            var derivedTypes = Subject.GetDerivedTypes(personType);
            derivedTypes.Count().Should().Be(4);

            var spouseType = derivedTypes.Single(x => x.InstanceType == typeof(Spouse));
            Subject.HasDerivedTypes(spouseType).Should().BeFalse();

            var employeeType = derivedTypes.Single(x => x.InstanceType == typeof(Employee));
            Subject.HasDerivedTypes(employeeType).Should().BeTrue();
            derivedTypes = Subject.GetDerivedTypes(employeeType);
            derivedTypes.Count().Should().Be(2);

            var managerType = derivedTypes.Single(x => x.InstanceType == typeof(Manager));
            Subject.HasDerivedTypes(managerType).Should().BeFalse();

            var contractorType = derivedTypes.Single(x => x.InstanceType == typeof(Contractor));
            Subject.HasDerivedTypes(contractorType).Should().BeFalse();

            var personRefType = Subject.Types.Single(x => x.InstanceType == typeof(PersonRef));
            Subject.HasDerivedTypes(personRefType).Should().BeTrue();
            derivedTypes = Subject.GetDerivedTypes(personRefType);
            derivedTypes.Count().Should().Be(1);

            var spouseRefType = derivedTypes.Single(x => x.InstanceType == typeof(SpouseRef));
            Subject.HasDerivedTypes(spouseRefType).Should().BeFalse();
        }

        [Test]
        public void should_map_each_type_correctly()
        {
            Subject.Types.Count().Should().Be(8);

            var personType = Subject.Types.Single(x => x.InstanceType == typeof(Person));
            personType.IsReadOnly.Should().BeTrue();
            personType.BaseType.Should().BeNull();
            personType.KeyProperties.Count.Should().Be(1);
            personType.KeyProperties.Should().Contain(x => x.Name == "Id");
            var personProperties = personType.PropertiesDeclaredOnThisType;
            personProperties.Count.Should().Be(2);
            personProperties.Should().Contain(x => x.Name == "Id");
            personProperties.Should().Contain(x => x.Name == "Name");

            var employeeType = Subject.Types.Single(x => x.InstanceType == typeof(Employee));
            employeeType.IsReadOnly.Should().BeTrue();
            employeeType.BaseType.Should().Be(personType);
            var employeeProperties = employeeType.PropertiesDeclaredOnThisType;
            employeeProperties.Count.Should().Be(3);
            employeeProperties.Should().Contain(x => x.Name == "HireDate");
            employeeProperties.Should().Contain(x => x.Name == "Salary");
            employeeProperties.Should().Contain(x => x.Name == "Spouse");

            var spouseType = Subject.Types.Single(x => x.InstanceType == typeof(Spouse));
            spouseType.IsReadOnly.Should().BeTrue();
            spouseType.BaseType.Should().Be(personType);
            var spouseProperties = spouseType.PropertiesDeclaredOnThisType;
            spouseProperties.Count.Should().Be(1);
            spouseProperties.Should().Contain(x => x.Name == "SpousesId");

            var managerType = Subject.Types.Single(x => x.InstanceType == typeof(Manager));
            managerType.IsReadOnly.Should().BeTrue();
            managerType.BaseType.Should().Be(employeeType);
            var managerProperties = managerType.PropertiesDeclaredOnThisType;
            managerProperties.Count.Should().Be(1);
            managerProperties.Should().Contain(x => x.Name == "Employees");

            var contractorType = Subject.Types.Single(x => x.InstanceType == typeof(Contractor));
            contractorType.IsReadOnly.Should().BeTrue();
            contractorType.BaseType.Should().Be(employeeType);
            var contractorProperties = contractorType.PropertiesDeclaredOnThisType;
            contractorProperties.Count.Should().Be(1);
            contractorProperties.Should().Contain(x => x.Name == "Address");

            var nameType = Subject.Types.Single(x => x.InstanceType == typeof(Name));
            nameType.IsReadOnly.Should().BeTrue();
            nameType.BaseType.Should().BeNull();
            nameType.KeyProperties.Count.Should().Be(0);
            var nameProperties = nameType.PropertiesDeclaredOnThisType;
            nameProperties.Count.Should().Be(2);
            nameProperties.Should().Contain(x => x.Name == "First");
            nameProperties.Should().Contain(x => x.Name == "Last");

            var personRefType = Subject.Types.Single(x => x.InstanceType == typeof(PersonRef));
            personRefType.IsReadOnly.Should().BeTrue();
            personRefType.BaseType.Should().BeNull();
            personRefType.KeyProperties.Count.Should().Be(0);
            var personRefProperties = personRefType.PropertiesDeclaredOnThisType;
            personRefProperties.Count.Should().Be(2);
            personRefProperties.Should().Contain(x => x.Name == "Id");
            personRefProperties.Should().Contain(x => x.Name == "Name");

            var spouseRefType = Subject.Types.Single(x => x.InstanceType == typeof(SpouseRef));
            spouseRefType.IsReadOnly.Should().BeTrue();
            spouseRefType.BaseType.Should().Be(personRefType);
            var spouseRefProperties = spouseRefType.PropertiesDeclaredOnThisType;
            spouseRefProperties.Count.Should().Be(1);
            spouseRefProperties.Should().Contain(x => x.Name == "MarriageDate");
        }

        private class Hierarchy
        {
            public MongoCollection<Person> People { get; set; }
        }

        [BsonKnownTypes(typeof(Employee), typeof(Spouse))]
        private abstract class Person
        {
            public string Id { get; set;}

            public Name Name { get; set;}
        }

        private class Name 
        {
            public string First { get; set;}

            public string Last { get; set;}
        }

        [BsonKnownTypes(typeof(Manager), typeof(Contractor))]
        private class Employee : Person
        {
            public DateTime HireDate { get; set; }

            public bool IsMarried
            {
                get { return Spouse == null; }
            }

            public int Salary { get; set; }

            public SpouseRef Spouse { get; set; }
        }

        private class Spouse : Person
        {
            public string SpousesId { get; set; }
        }

        private class Manager : Employee
        {
            public List<PersonRef> Employees { get; set; }
        }

        private class Contractor : Employee
        {
            public string Address { get; set; }
        }

        private class PersonRef
        {
            public string Id { get; set; }

            public Name Name { get; set; }
        }

        private class SpouseRef : PersonRef
        {
            public DateTime MarriageDate { get; set; }
        }
    }
}