using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;

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
            var objectCreated = refObject.Create(() => new object());

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
                var objectCreated = refObject.Create(() => null);
            }
            catch (Exception ex)
            {
                // assert
                ex.Should().BeOfType(typeof(ArgumentException));
            }
        }

        public void ReferenceCounter_InvalidState()
        {
            // arrange
            var refObject = new ReferenceCounter<object>();
            // act
            try
            {
                var objectCreated = refObject.Create(() => null);
            }
            catch (Exception ex)
            {
                // assert
                ex.Should().BeOfType(typeof(ArgumentException));
            }
        }

        [TestMethod()]
        public void ReferenceCounter_CreateOneObjectAndTestRemovalExecution()
        {
            // arrange
            var refObject = new ReferenceCounter<MockableTestClass>();

            // act
            var objectCreated = refObject.CreateWithRemoveAction(() => new MockableTestClass(), (obj) =>
            {
                var t = new Task(() => obj.BooleanProperty = true);
                t.RunSynchronously();
                return t;
            });

            // assert
            refObject.Count.Should().Be(1);
            objectCreated.Should().BeSameAs(refObject.Value);
            objectCreated.BooleanProperty.Should().BeFalse();

            // act
            refObject.Remove();

            // assert
            refObject.Count.Should().Be(0);
            objectCreated.BooleanProperty.Should().BeTrue();
            refObject.Value.Should().BeNull();
        }

        [TestMethod()]
        public void ReferenceCounter_CreateTwoObjectsAndTestRemovalExecution()
        {
            // arrange
            var refObject = new ReferenceCounter<MockableTestClass>();

            // act
            var objectCreated = refObject.CreateWithRemoveAction(() => new MockableTestClass(), (obj) =>
            {
                var t = new Task(() => obj.BooleanProperty = true);
                t.RunSynchronously();
                return t;
            });

            // assert
            objectCreated.Should().BeSameAs(refObject.Value);
            refObject.Count.Should().Be(1);

            // act
            // act
            var objectCreated2 = refObject.CreateWithRemoveAction(() => new MockableTestClass(), (obj) =>
            {
                var t = new Task(() => obj.BooleanProperty = true);
                t.RunSynchronously();
                return t;
            });

            // assert
            objectCreated.Should().BeSameAs(refObject.Value);
            refObject.Count.Should().Be(2);
            objectCreated.BooleanProperty.Should().BeFalse();

            // act
            refObject.Remove();

            // assert
            refObject.Count.Should().Be(1);
            objectCreated.BooleanProperty.Should().BeFalse();

            // act
            refObject.Remove();

            // assert
            refObject.Count.Should().Be(0);
            objectCreated.BooleanProperty.Should().BeTrue();
            refObject.Value.Should().BeNull();
        }


        [TestMethod()]
        public void ReferenceCounter_ClearTest()
        {
            // arrange
            var refObject = new ReferenceCounter<MockableTestClass>();

            // act 
            // act
            var objectCreated = refObject.CreateWithRemoveAction(() => new MockableTestClass(), (obj) =>
            {
                var t = new Task(() => obj.BooleanProperty = true);
                t.RunSynchronously();
                return t;
            });

            // assert
            objectCreated.Should().BeSameAs(refObject.Value);
            refObject.Count.Should().Be(1);

            // act
            // act
            var objectCreated2 = refObject.CreateWithRemoveAction(() => new MockableTestClass(), (obj) =>
            {
                var t = new Task(() => obj.BooleanProperty = true);
                t.RunSynchronously();
                return t;
            });

            // assert
            objectCreated.Should().BeSameAs(refObject.Value);
            refObject.Count.Should().Be(2);
            objectCreated.BooleanProperty.Should().BeFalse();

            // act
            refObject.Clear();

            // assert
            refObject.Count.Should().Be(0);
            objectCreated.BooleanProperty.Should().BeTrue();
            refObject.Value.Should().BeNull();
        }

        [TestMethod()]
        public void ReferenceCounter_RemoveOneObjectManyTimesTest()
        {
            // arrange
            var refObject = new ReferenceCounter<MockableTestClass>();

            // act
            // act
            var objectCreated = refObject.CreateWithRemoveAction(() => new MockableTestClass(), (obj) =>
            {
                var t = new Task(() => obj.BooleanProperty = true);
                t.RunSynchronously();
                return t;
            });

            // assert
            refObject.Count.Should().Be(1);
            objectCreated.Should().BeSameAs(refObject.Value);
            objectCreated.BooleanProperty.Should().BeFalse();

            // act
            refObject.Remove();

            // assert
            refObject.Count.Should().Be(0);

            // act
            refObject.Remove();

            // assert
            refObject.Count.Should().Be(0);

            // act
            refObject.Remove();

            // assert
            refObject.Count.Should().Be(0);

            // act
            refObject.Remove();

            // assert
            refObject.Count.Should().Be(0);
            objectCreated.BooleanProperty.Should().BeTrue();
            refObject.Value.Should().BeNull();
        }

        [TestMethod()]
        public void ReferenceCounter_RemoveAndClearOneObjectManyTimesTest()
        {
            // arrange
            var refObject = new ReferenceCounter<MockableTestClass>();

            // act
            // act
            var objectCreated = refObject.CreateWithRemoveAction(() => new MockableTestClass(), (obj) =>
            {
                var t = new Task(() => obj.BooleanProperty = true);
                t.RunSynchronously();
                return t;
            });

            // assert
            refObject.Count.Should().Be(1);
            objectCreated.Should().BeSameAs(refObject.Value);
            objectCreated.BooleanProperty.Should().BeFalse();

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
            objectCreated.BooleanProperty.Should().BeTrue();
            refObject.Value.Should().BeNull();
        }

        [TestMethod()]
        public void ReferenceCounter_CreateAfterLastRemove()
        {
            // arrange
            var refObject = new ReferenceCounter<MockableTestClass>();

            // act
            // act
            var objectCreated = refObject.CreateWithRemoveAction(() => new MockableTestClass(), (obj) =>
            {
                var t = new Task(() => obj.BooleanProperty = true);
                t.RunSynchronously();
                return t;
            });

            // assert
            refObject.Count.Should().Be(1);
            objectCreated.Should().BeSameAs(refObject.Value);
            objectCreated.BooleanProperty.Should().BeFalse();

            // act
            refObject.Remove();

            // assert
            refObject.Count.Should().Be(0);
            objectCreated.BooleanProperty.Should().BeTrue();
            refObject.Value.Should().BeNull();

            // act
            objectCreated = refObject.CreateWithRemoveAction(() => new MockableTestClass(), (obj) =>
            {
                var t = new Task(() => obj.BooleanProperty = true);
                t.RunSynchronously();
                return t;
            });

            // assert
            refObject.Count.Should().Be(1);
            objectCreated.Should().BeSameAs(refObject.Value);
            objectCreated.BooleanProperty.Should().BeFalse();

            // act
            refObject.Remove();

            // assert
            refObject.Count.Should().Be(0);
            objectCreated.BooleanProperty.Should().BeTrue();
            refObject.Value.Should().BeNull();
        }

    }
}