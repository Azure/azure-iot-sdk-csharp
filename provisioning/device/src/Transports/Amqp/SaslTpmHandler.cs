// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//using System;
//using System.Diagnostics;
//using System.Text;
//using Microsoft.Azure.Amqp;
//using Microsoft.Azure.Amqp.Sasl;
//using Microsoft.Azure.Devices.Authentication;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    // Commented out until we resolve plans for TPM support and library dependency
    //internal class SaslTpmHandler : SaslHandler
    //{
    //    private static readonly byte[] EmptyByte = { 0 };
    //    private const string MechanismName = "TPM";

    //    private readonly byte[] _endorsementKey;

    //    private readonly string _idScope;
    //    private readonly AuthenticationProviderTpm _authentication;
    //    private readonly byte[] _storageRootKey;
    //    private byte[] _nonceBuffer = Array.Empty<byte>();
    //    private byte _nextSequenceNumber;
    //    private string _hostName => $"{_idScope}/registrations/{_authentication.GetRegistrationId()}";

    //    public SaslTpmHandler(
    //        byte[] endorsementKey,
    //        byte[] storageRootKey,
    //        string idScope,
    //        AuthenticationProviderTpm authentication)
    //    {
    //        Debug.Assert(endorsementKey != null);
    //        Debug.Assert(storageRootKey != null);
    //        Debug.Assert(!string.IsNullOrWhiteSpace(idScope));
    //        Debug.Assert(authentication != null);

    //        Mechanism = MechanismName;
    //        _endorsementKey = endorsementKey;
    //        _storageRootKey = storageRootKey;
    //        _idScope = idScope;
    //        _authentication = authentication;
    //    }

    //    protected bool Equals(SaslTpmHandler other)
    //    {
    //        return
    //            Equals(_endorsementKey, other._endorsementKey) &&
    //            Equals(_storageRootKey, other._storageRootKey) &&
    //            string.CompareOrdinal(_idScope, other._idScope) == 0 &&
    //            Equals(_authentication, other._authentication);
    //    }

    //    public override bool Equals(object obj)
    //    {
    //        if (obj is null)
    //        {
    //            return false;
    //        }

    //        if (ReferenceEquals(this, obj))
    //        {
    //            return true;
    //        }

    //        if (obj.GetType() != GetType())
    //        {
    //            return false;
    //        }

    //        return Equals((SaslTpmHandler)obj);
    //    }

    //    public override int GetHashCode()
    //    {
    //        unchecked
    //        {
    //            int hashCode = _endorsementKey != null ? _endorsementKey.GetHashCode() : 0;
    //            hashCode = (hashCode * 397) ^ (_storageRootKey != null ? _storageRootKey.GetHashCode() : 0);
    //            hashCode = (hashCode * 397) ^ (_idScope != null ? _idScope.GetHashCode() : 0);
    //            hashCode = (hashCode * 397) ^ (_authentication != null ? _authentication.GetHashCode() : 0);

    //            return hashCode;
    //        }
    //    }

    //    public override SaslHandler Clone()
    //    {
    //        return new SaslTpmHandler(_endorsementKey, _storageRootKey, _idScope, _authentication);
    //    }

    //    public override void OnChallenge(SaslChallenge challenge)
    //    {
    //        SaslChallengeAction challengeAction = GetChallengeAction(challenge);

    //        switch (challengeAction)
    //        {
    //            case SaslChallengeAction.First:
    //                SendStorageRootKey();
    //                break;

    //            case SaslChallengeAction.Interim:
    //                SaveEncodedNonceSegment(challenge);
    //                SendInterimResponse();
    //                break;

    //            case SaslChallengeAction.Final:
    //                SaveEncodedNonceSegment(challenge);
    //                SendLastResponse();
    //                break;
    //        }
    //    }

    //    private void SendInterimResponse()
    //    {
    //        var response = new SaslResponse { Response = new ArraySegment<byte>(EmptyByte) };
    //        Negotiator.WriteFrame(response, true);
    //    }

    //    private void SendLastResponse()
    //    {
    //        string sas = ProvisioningSasBuilder.ExtractServiceAuthKey(
    //            _authentication,
    //            _hostName,
    //            _nonceBuffer);

    //        byte[] responseBuffer = new byte[sas.Length + 1];
    //        responseBuffer[0] = 0x0;
    //        Buffer.BlockCopy(Encoding.UTF8.GetBytes(sas), 0, responseBuffer, 1, sas.Length);

    //        var response = new SaslResponse { Response = new ArraySegment<byte>(responseBuffer) };
    //        Negotiator.WriteFrame(response, true);
    //    }

    //    private void SaveEncodedNonceSegment(SaslChallenge saslChallenge)
    //    {
    //        byte sequenceNumber = GetSequenceNumber(saslChallenge);
    //        if (_nextSequenceNumber != sequenceNumber)
    //        {
    //            throw new AmqpException(AmqpErrorCode.InvalidField,
    //                $"Invalid sequence number, expected: {_nextSequenceNumber}, actual: {sequenceNumber}.");
    //        }

    //        byte[] tempNonce = new byte[_nonceBuffer.Length];
    //        Buffer.BlockCopy(_nonceBuffer, 0, tempNonce, 0, _nonceBuffer.Length);

    //        _nonceBuffer = new byte[_nonceBuffer.Length + saslChallenge.Challenge.Array.Length - 1];
    //        Buffer.BlockCopy(tempNonce, 0, _nonceBuffer, 0, tempNonce.Length);
    //        Buffer.BlockCopy(
    //            saslChallenge.Challenge.Array,
    //            1,
    //            _nonceBuffer,
    //            tempNonce.Length,
    //            saslChallenge.Challenge.Array.Length - 1);
    //        _nextSequenceNumber++;
    //    }

    //    private static byte GetSequenceNumber(SaslChallenge saslChallenge)
    //    {
    //        return (byte)(saslChallenge.Challenge.Array[0] & SaslControlByteMask.SequenceNumber);
    //    }

    //    private void SendStorageRootKey()
    //    {
    //        var response = new SaslResponse { Response = new ArraySegment<byte>(CreateStorageRootKeyMessage()) };
    //        Negotiator.WriteFrame(response, true);
    //    }

    //    private byte[] CreateStorageRootKeyMessage()
    //    {
    //        byte[] responseBuffer = new byte[_storageRootKey.Length + 1];
    //        responseBuffer[0] = 0x0;
    //        Buffer.BlockCopy(_storageRootKey, 0, responseBuffer, 1, _storageRootKey.Length);
    //        return responseBuffer;
    //    }

    //    private static SaslChallengeAction GetChallengeAction(SaslChallenge saslChallenge)
    //    {
    //        byte[] challengeBytes = saslChallenge.Challenge.Array;

    //        if (challengeBytes == null || challengeBytes.Length == 0)
    //        {
    //            throw new AmqpException(AmqpErrorCode.InvalidField, "Sasl challenge message is missing or empty.");
    //        }

    //        if (challengeBytes[0] == 0x0)
    //        {
    //            return SaslChallengeAction.First;
    //        }

    //        if ((challengeBytes[0] & SaslControlByteMask.InterimSegment) != 0x0)
    //        {
    //            return (challengeBytes[0] & SaslControlByteMask.FinalSegment) != 0x0
    //                ? SaslChallengeAction.Final
    //                : SaslChallengeAction.Interim;
    //        }

    //        throw new AmqpException(AmqpErrorCode.InvalidField,
    //            $"Sasl challenge control byte contains invalid data: {challengeBytes[0]:X}.");
    //    }

    //    public override void OnResponse(SaslResponse response)
    //    {
    //        //This method is only implemented on the server
    //        Debug.Fail("OnResponse is not implemented on client");
    //    }

    //    protected override void OnStart(SaslInit init, bool isClient)
    //    {
    //        Debug.Assert(isClient);
    //        SendSaslInitMessage(init);
    //    }

    //    private void SendSaslInitMessage(SaslInit init)
    //    {
    //        init.InitialResponse = new ArraySegment<byte>(CreateSaslInitMessage(init));
    //        Negotiator.WriteFrame(init, true);
    //    }

    //    private byte[] CreateSaslInitMessage(SaslInit init)
    //    {
    //        init.HostName = _hostName;
    //        var initContent = new StringBuilder();
    //        initContent.Append(_idScope);
    //        initContent.Append('\0');
    //        initContent.Append(_authentication.GetRegistrationId());
    //        initContent.Append('\0');

    //        byte[] initContentInBytes = Encoding.UTF8.GetBytes(initContent.ToString());

    //        byte[] responseBuffer = new byte[initContentInBytes.Length + _endorsementKey.Length + 1];
    //        responseBuffer[0] = 0x0;
    //        Buffer.BlockCopy(initContentInBytes, 0, responseBuffer, 1, initContentInBytes.Length);
    //        Buffer.BlockCopy(_endorsementKey, 0, responseBuffer, initContentInBytes.Length + 1, _endorsementKey.Length);
    //        return responseBuffer;
    //    }

    //    //         Control byte format
    //    //    MSB     7        6   5   4   3     2  LSB
    //    // Presence  Final       Undefined      Sequence
    //    //           Segment                    Number
    //    private static class SaslControlByteMask
    //    {
    //        public const byte InterimSegment = 0x80;
    //        public const byte FinalSegment = 0x40;
    //        public const byte SequenceNumber = 0x03;
    //    }

    //    private enum SaslChallengeAction
    //    {
    //        First,
    //        Interim,
    //        Final,
    //    }
    //}
}
