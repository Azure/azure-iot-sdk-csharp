// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The data structure represent the Device Twin Method that is used for triggering an activity on the device.
    /// </summary>
    public sealed class MethodResponse
    {
        private byte[] _result;

        /// <summary>
        /// Make a new instance of the return class and validates that the payload is correct JSON.
        /// </summary>
        /// <param name="result">data returned by the method call.</param>
        /// <param name="status">status indicating success or failure.</param>
        /// <returns></returns>
        public MethodResponse(byte[] result, int status)
        {
            Result = result;
            Status = status;
        }

        /// <summary>
        /// Constructor which uses the input byte array as the body.
        /// </summary>
        /// <param name="status">An integer code containing a method call status.</param>
        public MethodResponse(int status)
        {
            Status = status;
        }

        /// <summary>
        /// Property containing entire result data. The formatting is checked for JSON correctness
        /// upon setting this property.
        /// </summary>
        [SuppressMessage(
            "Performance",
            "CA1819:Properties should not return arrays",
            Justification = "Cannot change property types on public classes.")]
        public byte[] Result
        {
            private set
            {
                Utils.ValidateDataIsEmptyOrJson(value);

                _result = value;
            }
            get => _result;
        }

        /// <summary>
        /// Property containing the entire result data, in JSON format.
        /// </summary>
        public string ResultAsJson => Result == null || Result.Length == 0
            ? null
            : Encoding.UTF8.GetString(Result);

        /// <summary>
        /// The response of the device client application method handler.
        /// </summary>
        public int Status { get; private set; }
    }
}
