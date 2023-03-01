// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the thirdpartynotice.txt file for more information.

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace Microsoft.Azure.Devices.Client.HsmAuthentication.Transport
{
    internal sealed class UnixDomainSocketEndPoint : EndPoint
    {
        private const AddressFamily EndPointAddressFamily = AddressFamily.Unix;

        private static readonly Encoding s_pathEncoding = Encoding.UTF8;

        private const int NativePathOffset = 2; // = offset of(struct sockaddr_un, sun_path). It's the same on Linux and OSX

        private const int NativePathLength = 91; // sockaddr_un.sun_path at http://pubs.opengroup.org/onlinepubs/9699919799/basedefs/sys_un.h.html, -1 for terminator

        private const int NativeAddressSize = NativePathOffset + NativePathLength;

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

        internal UnixDomainSocketEndPoint(SocketAddress socketAddress)
        {
            if (socketAddress == null)
            {
                throw new ArgumentNullException(nameof(socketAddress));
            }

            if (socketAddress.Family != EndPointAddressFamily
                || socketAddress.Size > NativeAddressSize)
            {
                throw new ArgumentOutOfRangeException(nameof(socketAddress));
            }

            if (socketAddress.Size > NativePathOffset)
            {
                _encodedPath = new byte[socketAddress.Size - NativePathOffset];
                for (int i = 0; i < _encodedPath.Length; i++)
                {
                    _encodedPath[i] = socketAddress[NativePathOffset + i];
                }

                _path = s_pathEncoding.GetString(_encodedPath, 0, _encodedPath.Length);
            }
            else
            {
                _encodedPath = Array.Empty<byte>();
                _path = string.Empty;
            }
        }

        /// <summary>
        /// Do not remove. Even though there are no references, it will be called by System.Net.HttpClient.
        /// </summary>
        public override SocketAddress Serialize()
        {
            var result = new SocketAddress(AddressFamily.Unix, NativeAddressSize);
            Debug.Assert(_encodedPath.Length + NativePathOffset <= result.Size, "Expected path to fit in address");

            for (int index = 0; index < _encodedPath.Length; index++)
            {
                result[NativePathOffset + index] = _encodedPath[index];
            }
            result[NativePathOffset + _encodedPath.Length] = 0; // path must be null-terminated

            return result;
        }


        /// <summary>
        /// Do not remove. Even though there are no references, it will be called by System.Net.HttpClient.
        /// </summary>
        public override EndPoint Create(SocketAddress socketAddress) => new UnixDomainSocketEndPoint(socketAddress);


        /// <summary>
        /// Do not remove. Even though there are no references, it will be called by System.Net.HttpClient.
        /// </summary>
        public override AddressFamily AddressFamily => EndPointAddressFamily;


        /// <summary>
        /// Do not remove. Even though there are no references, it will be called by System.Net.HttpClient.
        /// </summary>
        public override string ToString() => _path;
    }
}
