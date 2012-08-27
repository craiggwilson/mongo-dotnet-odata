using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MongoDB.Bson.Serialization.Attributes;

using NUnit.Framework;
using FluentAssertions;
using System.Data.Services.Providers;
using MongoDB.Bson.Serialization;

namespace MongoDB.OData.UnitTests
{
    [TestFixture]
    public class When_mapping_a_hierarchy : Specification<TypedMongoDataServiceMetadataBuilder>
    {
        private TypedMongoDataServiceMetadataProvider _provider;

        protected override TypedMongoDataServiceMetadataBuilder EstablishContext()
        {
            return new TypedMongoDataServiceMetadataBuilder();
        }

        protected override void Because()
        {
            Subject.AddResourceSet<Person>("People", "hr", "people");
            _provider = Subject.CreateMetadataProvider();
        }

        [Test]
        public void should_add_only_1_resource_set()
        {
            _provider.ResourceSets.Count().Should().Be(1);
        }

        [Test]
        public void should_map_the_resource_set()
        {
            var set = _provider.ResourceSets.Single();
            set.ResourceType.InstanceType.Should().Be(typeof(Person));
            set.ResourceType.IsAbstract.Should().BeTrue();
        }

        [Test]
        public void should_recognize_the_derived_types()
        {
            var personType = _provider.Types.Single(x => x.InstanceType == typeof(Person));
            _provider.HasDerivedTypes(personType).Should().BeTrue();
            var derivedTypes = _provider.GetDerivedTypes(personType);
            derivedTypes.Count().Should().Be(4);

            var spouseType = derivedTypes.Single(x => x.InstanceType == typeof(Spouse));
            _provider.HasDerivedTypes(spouseType).Should().BeFalse();

            var employeeType = derivedTypes.Single(x => x.InstanceType == typeof(Employee));
            _provider.HasDerivedTypes(employeeType).Should().BeTrue();
            derivedTypes = _provider.GetDerivedTypes(employeeType);
            derivedTypes.Count().Should().Be(2);

            var managerType = derivedTypes.Single(x => x.InstanceType == typeof(Manager));
            _provider.HasDerivedTypes(managerType).Should().BeFalse();

            var contractorType = derivedTypes.Single(x => x.InstanceType == typeof(Contractor));
            _provider.HasDerivedTypes(contractorType).Should().BeFalse();

            var personRefType = _provider.Types.Single(x => x.InstanceType == typeof(PersonRef));
            _provider.HasDerivedTypes(personRefType).Should().BeTrue();
            derivedTypes = _provider.GetDerivedTypes(personRefType);
            derivedTypes.Count().Should().Be(1);

            var spouseRefType = derivedTypes.Single(x => x.InstanceType == typeof(SpouseRef));
            _provider.HasDerivedTypes(spouseRefType).Should().BeFalse();
        }

        [Test]
        public void should_map_each_type_correctly()
        {
            _provider.Types.Count().Should().Be(8);

            var personType = _provider.Types.Single(x => x.InstanceType == typeof(Person));
            personType.IsReadOnly.Should().BeTrue();
            personType.BaseType.Should().BeNull();
            personType.KeyProperties.Count.Should().Be(1);
            personType.KeyProperties.Should().Contain(x => x.Name == "Id");
            var personProperties = personType.PropertiesDeclaredOnThisType;
            personProperties.Count.Should().Be(2);
            personProperties.Should().Contain(x => x.Name == "Id");
            personProperties.Should().Contain(x => x.Name == "Name");

            var employeeType = _provider.Types.Single(x => x.InstanceType == typeof(Employee));
            employeeType.IsReadOnly.Should().BeTrue();
            employeeType.BaseType.Should().Be(personType);
            var employeeProperties = employeeType.PropertiesDeclaredOnThisType;
            employeeProperties.Count.Should().Be(3);
            employeeProperties.Should().Contain(x => x.Name == "HireDate");
            employeeProperties.Should().Contain(x => x.Name == "Salary");
            employeeProperties.Should().Contain(x => x.Name == "Spouse");

            var spouseType = _provider.Types.Single(x => x.InstanceType == typeof(Spouse));
            spouseType.IsReadOnly.Should().BeTrue();
            spouseType.BaseType.Should().Be(personType);
            var spouseProperties = spouseType.PropertiesDeclaredOnThisType;
            spouseProperties.Count.Should().Be(1);
            spouseProperties.Should().Contain(x => x.Name == "SpousesId");

            var managerType = _provider.Types.Single(x => x.InstanceType == typeof(Manager));
            managerType.IsReadOnly.Should().BeTrue();
            managerType.BaseType.Should().Be(employeeType);
            var managerProperties = managerType.PropertiesDeclaredOnThisType;
            managerProperties.Count.Should().Be(1);
            managerProperties.Should().Contain(x => x.Name == "Employees");

            var contractorType = _provider.Types.Single(x => x.InstanceType == typeof(Contractor));
            contractorType.IsReadOnly.Should().BeTrue();
            contractorType.BaseType.Should().Be(employeeType);
            var contractorProperties = contractorType.PropertiesDeclaredOnThisType;
            contractorProperties.Count.Should().Be(1);
            contractorProperties.Should().Contain(x => x.Name == "Address");

            var nameType = _provider.Types.Single(x => x.InstanceType == typeof(Name));
            nameType.IsReadOnly.Should().BeTrue();
            nameType.BaseType.Should().BeNull();
            nameType.KeyProperties.Count.Should().Be(0);
            var nameProperties = nameType.PropertiesDeclaredOnThisType;
            nameProperties.Count.Should().Be(2);
            nameProperties.Should().Contain(x => x.Name == "First");
            nameProperties.Should().Contain(x => x.Name == "Last");

            var personRefType = _provider.Types.Single(x => x.InstanceType == typeof(PersonRef));
            personRefType.IsReadOnly.Should().BeTrue();
            personRefType.BaseType.Should().BeNull();
            personRefType.KeyProperties.Count.Should().Be(0);
            var personRefProperties = personRefType.PropertiesDeclaredOnThisType;
            personRefProperties.Count.Should().Be(2);
            personRefProperties.Should().Contain(x => x.Name == "Id");
            personRefProperties.Should().Contain(x => x.Name == "Name");

            var spouseRefType = _provider.Types.Single(x => x.InstanceType == typeof(SpouseRef));
            spouseRefType.IsReadOnly.Should().BeTrue();
            spouseRefType.BaseType.Should().Be(personRefType);
            var spouseRefProperties = spouseRefType.PropertiesDeclaredOnThisType;
            spouseRefProperties.Count.Should().Be(1);
            spouseRefProperties.Should().Contain(x => x.Name == "MarriageDate");
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