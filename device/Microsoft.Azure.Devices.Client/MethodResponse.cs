// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The data structure represent the Device Twin Method that is used for triggering an activity on the device
    /// </summary>
    public sealed class MethodResponse
    {
        byte[] result;

        /// <summary>
        /// Make a new instance of the return class and validates that the payload is correct JSON.
        /// </summary>
        /// <param name="result">data returned by the method call.</param>
        /// <param name="status">status indicating success or failure.</param>
        /// <returns></returns>
#if NETMF || NETSTANDARD1_3
        public MethodResponse(byte[] result, int status)
#else
        public MethodResponse([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArrayAttribute] byte[] result, int status)
#endif
        {
            this.Result = result;
            this.Status = status;
        }

        /// <summary>
        /// Constructor which uses the input byte array as the body
        /// </summary>
        /// <param name="status">an integer code contianing a method call status.</param>
        public MethodResponse(int status)
        {
            this.Status = status;
        }

        /// <summary>
        /// Property containing entire result data. The formatting is checked for JSON correctness
        /// upon setting this property.
        /// </summary>
        internal byte[] Result
        {
            private set
            {
                // codes_SRS_METHODCALLBACKRETURN_10_002: [ Result shall check if the input is validate JSON ]
                // codes_SRS_METHODCALLBACKRETURN_10_003: [ Result shall percolate the invalid token exception to the caller ]
                Utils.ValidateDataIsEmptyOrJson(value);

                this.result = value;
            }
            get
            {
                return this.result;
            }
        }

        /// <summary>
        /// contains the response of the device client application method handler.
        /// </summary>
        internal int Status
        {
            get; set;
        }
    }
}

