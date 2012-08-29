using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using System.Diagnostics;

namespace MongoDB.OData.UnitTests
{
    internal abstract class Specification<TSubject>
    {
        protected TSubject Subject { get; private set; }

        [TestFixtureSetUp]
        public void BaseSetUp()
        {
            Subject = EstablishContext();
            Because();
        }

        [TestFixtureTearDown]
        public void BaseTearDown()
        {
            DisposeContext();
        }

        [DebuggerStepThrough]
        protected abstract TSubject EstablishContext();

        [DebuggerStepThrough]
        protected virtual void Because() { }

        [DebuggerStepThrough]
        protected virtual void DisposeContext() { }
    }
}