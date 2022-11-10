// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the thirdpartynotice.txt file for more information.

using System;
using System.Text;
using System.Net;

namespace Microsoft.Azure.Devices.Client.HsmAuthentication.Transport
{
    internal sealed class UnixDomainSocketEndPoint : EndPoint
    {
        private const int NativePathLength = 91; // sockaddr_un.sun_path at http://pubs.opengroup.org/onlinepubs/9699919799/basedefs/sys_un.h.html, -1 for terminator
        private static readonly Encoding s_pathEncoding = Encoding.UTF8;

        private readonly string _path;
        private readonly byte[] _encodedPath;

        internal UnixDomainSocketEndPoint(string path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _encodedPath = s_pathEncoding.GetBytes(_path);

            if (path.Length == 0
                || _encodedPath.Length > NativePathLength)
            {
                throw new ArgumentOutOfRangeException(nameof(path), path);
            }
        }
    }
}
