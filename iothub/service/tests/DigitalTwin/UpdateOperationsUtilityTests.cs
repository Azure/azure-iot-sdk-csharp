// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Tests.DigitalTwin
{
    [TestClass]
    [TestCategory("Unit")]
    public class UpdateOperationsUtilityTests
    {
        private const string Op = "op";
        private const string Add = "add";
        private const string Replace = "replace";
        private const string Remove = "remove";
        private const string Value = "value";
        private const string Metadata = "$metadata";

        [TestMethod]
        public void UpdateUtilityAppendsAddPropertyOp()
        {
            var op = new UpdateOperationsUtility();
            string path = "testPath";
            int value = 10;

            op.AppendAddPropertyOp(path, value);
            string operations = op.Serialize();

            // There should be a single operation added.
            var jArray = JArray.Parse(operations);
            jArray.Count.Should().Be(1);

            // The patch operation added should be an "add" operation.
            JToken jObject = jArray.First;
            jObject.Value<string>(Op).Should().Be(Add);
        }

        [TestMethod]
        public void UpdateUtilityAppendsAddComponentOp()
        {
            var op = new UpdateOperationsUtility();
            string path = "testPath";
            string property = "someProperty";
            int value = 10;

            op.AppendAddComponentOp(path, new Dictionary<string, object> { { property, value } });
            string operations = op.Serialize();

            // There should be a single operation added.
            var jArray = JArray.Parse(operations);
            jArray.Count.Should().Be(1);

            // The patch operation added should be an "add" operation.
            JToken jObject = jArray.First;
            jObject.Value<string>(Op).Should().Be(Add);

            // The value should have a "$metadata" : {} mapping.
            JObject patchValue = jObject.Value<JObject>(Value);
            patchValue[Metadata].Should().NotBeNull();
            patchValue[Metadata].Should().BeEmpty();
        }

        [TestMethod]
        public void UpdateUtilityAppendsReplacePropertyOp()
        {
            var op = new UpdateOperationsUtility();
            string path = "testPath";
            int value = 10;

            op.AppendReplacePropertyOp(path, value);
            string operations = op.Serialize();

            // There should be a single operation added.
            var jArray = JArray.Parse(operations);
            jArray.Count.Should().Be(1);

            // The patch operation added should be a "replace" operation.
            JToken jObject = jArray.First;
            jObject.Value<string>(Op).Should().Be(Replace);
        }

        [TestMethod]
        public void UpdateUtilityAppendsReplaceComponentOp()
        {
            var op = new UpdateOperationsUtility();
            string path = "testPath";
            string property = "someProperty";
            int value = 10;

            op.AppendReplaceComponentOp(path, new Dictionary<string, object> { { property, value } });
            string operations = op.Serialize();

            // There should be a single operation added.
            var jArray = JArray.Parse(operations);
            jArray.Count.Should().Be(1);

            // The patch operation added should be a "replace" operation.
            JToken jObject = jArray.First;
            jObject.Value<string>(Op).Should().Be(Replace);

            // The value should have a "$metadata" : {} mapping.
            JObject patchValue = jObject.Value<JObject>(Value);
            patchValue[Metadata].Should().NotBeNull();
            patchValue[Metadata].Should().BeEmpty();
        }

        [TestMethod]
        public void UpdateUtilityAppendsRemoveOp()
        {
            var op = new UpdateOperationsUtility();
            string path = "testPath";

            op.AppendRemoveOp(path);
            string operations = op.Serialize();

            // There should be a single operation added.
            var jArray = JArray.Parse(operations);
            jArray.Count.Should().Be(1);

            // The patch operation added should be a "remove" operation.
            JToken jObject = jArray.First;
            jObject.Value<string>(Op).Should().Be(Remove);
        }

        [TestMethod]
        public void UpdateUtilityAppendMultipleOperations()
        {
            var op = new UpdateOperationsUtility();
            string addPath = "testPath1";
            int addValue = 10;
            string replacePath = "testpath2";
            int replaceValue = 20;

            op.AppendAddPropertyOp(addPath, addValue);
            op.AppendReplacePropertyOp(replacePath, replaceValue);
            string operations = op.Serialize();

            // There should be two operations added.
            var jArray = JArray.Parse(operations);
            jArray.Count.Should().Be(2);

            // The patch operation added should have an "add" and a "replace" operation.
            var expectedOperations = new List<string> { Add, Replace };
            var actualOperations = new List<string>();
            foreach (JObject item in jArray)
            {
                actualOperations.Add(item.Value<string>(Op));
            }
            actualOperations.Should().OnlyContain(item => expectedOperations.Contains(item));
        }
    }
}
