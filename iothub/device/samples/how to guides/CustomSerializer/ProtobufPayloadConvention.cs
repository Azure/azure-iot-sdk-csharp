// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Azure.Devices.Client;

namespace CustomSerializer
{
    internal class ProtobufPayloadConvention : PayloadConvention
    {
        private readonly MessageParser _parser;
        public ProtobufPayloadConvention(MessageParser parser) => _parser = parser;
        
        public override string ContentType => "proto";

        public override Encoding ContentEncoding => Encoding.Default;

        public override T GetObject<T>(string jsonObjectAsText)
        {
            throw new NotImplementedException();
        }

        public override T GetObject<T>(byte[] objectToConvert)
        {
            return (T)_parser.ParseFrom(objectToConvert);
        }

        public override Task<T> GetObjectAsync<T>(Stream streamToConvert)
        {
            throw new NotImplementedException();
        }

        public override byte[] GetObjectBytes(object objectToSendWithConvention)
        {
            return (objectToSendWithConvention as IMessage).ToByteArray();
        }
    }
}
