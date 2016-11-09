// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Schema;

    /// <summary>
    /// The data structure represent the Device Twin Method that is used for triggering an activity on the device
    /// </summary>
    public sealed class MethodCallbackReturn
    {
        byte[] result;

        /// <summary>
        /// Factory will make a new instance of the return class and validates that the payload
        /// is correct JSON. Throws.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="status"></param>
        /// <returns></returns>
#if NETMF
        public static MethodCallbackReturn MethodCallbackReturnFactory(byte[] payload, int status)
#else
        public static MethodCallbackReturn MethodCallbackReturnFactory([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArrayAttribute] byte[] result, int status)
#endif
        {
            /* codes_SRS_METHODCALLBACKRETURN_10_001: [ MethodCallbackReturnFactory shall instanciate a new MethodCallbackReturn with given properties. ] */
            MethodCallbackReturn retValue = new MethodCallbackReturn(status);
            retValue.Result = result;
            return retValue;
        }

        /// <summary>
        /// Constructor which uses the input byte array as the body
        /// </summary>
        /// <param name="status">an integer code contianing a method call status.</param>
        MethodCallbackReturn(int status)
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
                /* codes_SRS_METHODCALLBACKRETURN_10_002: [** Result shall check if the input is validate JSON ] */
                //JsonTextReader reader = new JsonTextReader(new StringReader(value));

                /* codes_SRS_METHODCALLBACKRETURN_10_003: [ Result shall percolate the invalid token exception to the caller ] */
                //while (reader.Read()) ;  // throws if not valid JSON

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

