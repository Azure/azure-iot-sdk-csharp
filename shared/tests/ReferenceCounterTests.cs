using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using System.Threading;

namespace Microsoft.Azure.Devices.Shared.Tests
{
    [TestClass()]
    public class ReferenceCounterTests
    {
        private class MockableTestClass
        {
            public bool BooleanProperty { get; set; }
        }

        [TestMethod()]
        public void ReferenceCounter_CreateOneObject()
        {
            // arrange
            var refObject = new ReferenceCounter<object>();

            // act
            var objectCreated = refObject.Create(new object());

            // assert
            objectCreated.Should().BeSameAs(refObject.Value);
        }

        public void ReferenceCounter_CreateOneNull()
        {
            // arrange
            var refObject = new ReferenceCounter<object>();

            // act
            try
            {
                var objectCreated = refObject.Create(null);
            }
            catch (Exception ex)
            {
                // assert
                ex.Should().BeOfType(typeof(ArgumentException));
            }
        }

        [TestMethod()]
        public void ReferenceCounter_ClearTest()
        {
            // arrange
            var refObject = new ReferenceCounter<object>();

            // act
            var objectCreated = refObject.Create(new object());

            // assert
            objectCreated.Should().BeSameAs(refObject.Value);
            refObject.Count.Should().Be(1);

            // act
            var objectCreated2 = refObject.Create(new object());

            // assert
            objectCreated2.Should().BeSameAs(refObject.Value);
            refObject.Count.Should().Be(2);

            // act
            var refObjectClear = refObject.Clear();

            // assert
            refObjectClear.Should().BeSameAs(objectCreated);
            refObject.Count.Should().Be(0);
            refObject.Value.Should().BeNull();
        }

        [TestMethod()]
        public void ReferenceCounter_RemoveOneObjectManyTimesTest()
        {
            // arrange
            var refObject = new ReferenceCounter<object>();

            // act
            var objectCreated = refObject.Create(new object());

            // assert
            refObject.Count.Should().Be(1);
            objectCreated.Should().BeSameAs(refObject.Value);

            // act
            var refObjectRemove = refObject.Remove();

            // assert
            refObjectRemove.Should().BeSameAs(objectCreated);
            refObject.Count.Should().Be(0);

            // act
            refObjectRemove = refObject.Remove();

            // assert
            refObjectRemove.Should().BeNull();
            refObject.Count.Should().Be(0);

            // act
            refObjectRemove = refObject.Remove();

            // assert
            refObjectRemove.Should().BeNull();
            refObject.Count.Should().Be(0);

            // act
            refObjectRemove = refObject.Remove();

            // assert
            refObject.Count.Should().Be(0);
            refObjectRemove.Should().BeNull();
            refObject.Value.Should().BeNull();
        }

        [TestMethod()]
        public void ReferenceCounter_RemoveAndClearOneObjectManyTimesTest()
        {
            // arrange
            var refObject = new ReferenceCounter<object>();

            // act
            var objectCreated = refObject.Create(new object());

            // assert
            refObject.Count.Should().Be(1);
            objectCreated.Should().BeSameAs(refObject.Value);

            // act
            refObject.Remove();
            refObject.Clear();

            // assert
            refObject.Count.Should().Be(0);

            // act
            refObject.Remove();
            refObject.Clear();


            // assert
            refObject.Count.Should().Be(0);

            // act
            refObject.Remove();
            refObject.Clear();


            // assert
            refObject.Count.Should().Be(0);

            // act
            refObject.Remove();
            refObject.Clear();

            // assert
            refObject.Count.Should().Be(0);
            refObject.Value.Should().BeNull();
        }

        [TestMethod()]
        public void ReferenceCounter_CreateAfterLastRemove()
        {
            // arrange
            var refObject = new ReferenceCounter<object>();

            // act
            // act
            var objectCreated = refObject.Create(new object());

            // assert
            refObject.Count.Should().Be(1);
            objectCreated.Should().BeSameAs(refObject.Value);

            // act
            refObject.Remove();

            // assert
            refObject.Count.Should().Be(0);
            refObject.Value.Should().BeNull();

            // act
            objectCreated = refObject.Create(new object());

            // assert
            refObject.Count.Should().Be(1);
            objectCreated.Should().BeSameAs(refObject.Value);

            // act
            var refObjectReturn = refObject.Remove();

            // assert
            refObjectReturn.Should().BeNull();
            refObject.Count.Should().Be(0);
            refObject.Value.Should().BeNull();
        }

        [TestMethod()]
        public void ReferenceCounter_ManyThreads()
        {

            // arrange
            var refObject = new ReferenceCounter<object>();

            int loopCounter = 10000;

            var manualResetForRemove = new ManualResetEventSlim();
            // act
            Parallel.For(0, loopCounter, (i) =>
            {
                refObject.Create(new object());
            });

            // act
            Parallel.For(0, loopCounter, (i) =>
            {
                refObject.Remove();
            });

            // assert
            refObject.Count.Should().Be(0);
            refObject.Value.Should().BeNull();
        }

    }
}