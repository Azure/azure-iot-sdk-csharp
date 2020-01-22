// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Net;

namespace Microsoft.Azure.Devices.Common.Authorization
{
    public class ModelSharedAccessSignatureBuilder : SharedAccessSignatureBuilder
    {
        public string RepositoryId { get; set; }

        public override string ToSignature()
        {
            return BuildSignatureForModelRepo(KeyName, Key, hostName, RepositoryId).ToString();
        }

        public StringBuilder BuildSignatureForModelRepo(string keyName, string key, string Hostname, string repositoryId)
        {
            var buffer = BuildSignature(keyName, key, Hostname);
            buffer.AppendFormat(
                CultureInfo.InvariantCulture,
                "&{0}={1}",
                ModelSharedAccessSignatureConstants.RepositoryIdFiledName,
                repositoryId);

            return buffer;

        }
    }
}
