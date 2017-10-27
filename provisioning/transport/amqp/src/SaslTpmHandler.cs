// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class SaslTpmHandler : SaslHandler
    {
        static readonly byte[] EmptyByte = { 0 };
        private const string MechanismName = "TPM";
        private const int ChallengeKeySegmentSize = 400;
        private readonly StringBuilder _encodedNonceStringBuilder = new StringBuilder(ChallengeKeySegmentSize * 3);

        private readonly byte[] _endorsementKey;

        private readonly string _hostName;
        private readonly SecurityClientHsmTpm _security;
        private readonly byte[] _storageRootKey;
        private byte _nextSequenceNumber;

        public SaslTpmHandler(
            byte[] endorsementKey, 
            byte[] storageRootKey, 
            string hostName,
            SecurityClientHsmTpm security)
        {
            Debug.Assert(endorsementKey != null);
            Debug.Assert(storageRootKey != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(hostName));
            Debug.Assert(security != null);

            Mechanism = MechanismName;
            _endorsementKey = endorsementKey;
            _storageRootKey = storageRootKey;
            _hostName = hostName;
            _security = security;
        }

        protected bool Equals(SaslTpmHandler other)
        {
            return
                Equals(_endorsementKey, other._endorsementKey) &&
                Equals(_storageRootKey, other._storageRootKey) &&
                string.Equals(_hostName, other._hostName, StringComparison.InvariantCulture) &&
                Equals(_encodedNonceStringBuilder, other._encodedNonceStringBuilder) &&
                Equals(_security, other._security);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SaslTpmHandler) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _endorsementKey != null ? _endorsementKey.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (_storageRootKey != null ? _storageRootKey.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (_hostName != null ? _hostName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (_encodedNonceStringBuilder != null ? _encodedNonceStringBuilder.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (_security != null ? _security.GetHashCode() : 0);

                return hashCode;
            }
        }

        public static bool operator ==(SaslTpmHandler left, SaslTpmHandler right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SaslTpmHandler left, SaslTpmHandler right)
        {
            return !Equals(left, right);
        }

        public override SaslHandler Clone()
        {
            return new SaslTpmHandler(_endorsementKey, _storageRootKey, _hostName, _security);
        }

        public override void OnChallenge(SaslChallenge challenge)
        {
            var challengeAction = GetChallengeAction(challenge);

            switch (challengeAction)
            {
                case SaslChallengeAction.First:
                    SendStorageRootKey();
                    break;
                case SaslChallengeAction.Interim:
                    SaveEncodedNonceSegment(challenge);
                    SendInterimResponse();
                    break;
                case SaslChallengeAction.Final:
                    SaveEncodedNonceSegment(challenge);
                    SendLastResponse();
                    break;
            }
        }

        private void SendInterimResponse()
        {
            var response = new SaslResponse {Response = new ArraySegment<byte>(EmptyByte)};
            Negotiator.WriteFrame(response, true);
        }

        private void SendLastResponse()
        {
            //Notes: The _encodedNonce from service would soon change to non base64 string
            var sasBytes = _security.Sign(Convert.FromBase64String(_encodedNonceStringBuilder.ToString()));
            var sas = Convert.ToBase64String(sasBytes);

            var responseBuffer = new byte[sas.Length + 1];
            responseBuffer[0] = 0x0;
            Buffer.BlockCopy(Encoding.UTF8.GetBytes(sas), 0, responseBuffer, 1, sas.Length);

            var response = new SaslResponse {Response = new ArraySegment<byte>(responseBuffer)};
            Negotiator.WriteFrame(response, true);
        }

        private void SaveEncodedNonceSegment(SaslChallenge saslChallenge)
        {
            var sequenceNumber = GetSequenceNumber(saslChallenge);
            if (_nextSequenceNumber != sequenceNumber)
                throw new AmqpException(AmqpErrorCode.InvalidField,
                    $"Invalid sequence number, expected: {_nextSequenceNumber}, actual: {sequenceNumber}.");

            var nonceSegment = Encoding.UTF8.GetString(saslChallenge.Challenge.Array, 1,
                saslChallenge.Challenge.Array.Length - 1);
            _encodedNonceStringBuilder.Append(nonceSegment);
            _nextSequenceNumber++;
        }

        private byte GetSequenceNumber(SaslChallenge saslChallenge)
        {
            return (byte) (saslChallenge.Challenge.Array[0] & SaslControlByteMask.SequenceNumber);
        }

        private void SendStorageRootKey()
        {
            var response = new SaslResponse {Response = new ArraySegment<byte>(CreateStorageRootKeyMessage())};
            Negotiator.WriteFrame(response, true);
        }

        private byte[] CreateStorageRootKeyMessage()
        {
            var responseBuffer = new byte[_storageRootKey.Length + 1];
            responseBuffer[0] = 0x0;
            Buffer.BlockCopy(_storageRootKey, 0, responseBuffer, 1, _storageRootKey.Length);
            return responseBuffer;
        }

        private SaslChallengeAction GetChallengeAction(SaslChallenge saslChallenge)
        {
            var challengeBytes = saslChallenge.Challenge.Array;

            if (challengeBytes == null || challengeBytes.Length == 0)
                throw new AmqpException(AmqpErrorCode.InvalidField, "Sasl challenge message is missing or empty.");

            if (challengeBytes[0] == 0x0)
                return SaslChallengeAction.First;

            if ((challengeBytes[0] & SaslControlByteMask.InterimSegment) != 0x0)
            {
                if ((challengeBytes[0] & SaslControlByteMask.FinalSegment) != 0x0)
                    return SaslChallengeAction.Final;

                return SaslChallengeAction.Interim;
            }

            throw new AmqpException(AmqpErrorCode.InvalidField,
                $"Sasl challenge control byte contains invalid data: {challengeBytes[0]:X}.");
        }

        public override void OnResponse(SaslResponse response)
        {
            //This method is only implemented on the server
            Debug.Fail("OnResponse is not implemented on client");
        }

        protected override void OnStart(SaslInit init, bool isClient)
        {
            Debug.Assert(isClient);
            SendSaslInitMessage(init);
        }

        private void SendSaslInitMessage(SaslInit init)
        {
            init.InitialResponse = new ArraySegment<byte>(CreateSaslInitMessage(init));
            Negotiator.WriteFrame(init, true);
        }

        private byte[] CreateSaslInitMessage(SaslInit init)
        {
            init.HostName = _hostName;
            var responseBuffer = new byte[_endorsementKey.Length + 1];
            responseBuffer[0] = 0x0;
            Buffer.BlockCopy(_endorsementKey, 0, responseBuffer, 1, _endorsementKey.Length);
            return responseBuffer;
        }

        //         Control byte format
        //    MSB     7        6   5   4   3     2  LSB
        // Presence  Final       Undefined      Sequence
        //           Segment                    Number
        private static class SaslControlByteMask
        {
            public const byte InterimSegment = 0x80;
            public const byte FinalSegment = 0x40;
            public const byte SequenceNumber = 0x03;
        }

        private enum SaslChallengeAction
        {
            First,
            Interim,
            Final
        }
    }
}
