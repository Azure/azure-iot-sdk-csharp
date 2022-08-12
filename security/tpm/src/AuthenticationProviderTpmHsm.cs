// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Azure.Devices.Authentication;
using Tpm2Lib;

namespace Microsoft.Azure.Devices.Provisioning.Security
{
    /// <summary>
    /// The Provisioning Authentication Client implementation for TPM.
    /// </summary>
    public class AuthenticationProviderTpmHsm : AuthenticationProviderTpm, IDisposable
    {
        private bool _disposed;

        private const uint Tpm20SrkHandle = ((uint)Ht.Persistent << 24) | 0x00000001;
        private const uint Tpm20EkHandle = ((uint)Ht.Persistent << 24) | 0x00010001;
        private const uint AiothPersistedUriIindex = ((uint)Ht.NvIndex << 24) | 0x00400100;
        private const uint AiothPersistedKeyHandle = ((uint)Ht.Persistent << 24) | 0x00000100;

        private Tpm2Device _tpmDevice;
        private Tpm2 _tpm2;

        // TPM identity cache
        private TpmPublic _ekPub;

        private TpmPublic _srkPub;
        private TpmPublic _idKeyPub;
        private TpmHandle _idKeyHandle = TpmHandle.RhNull;
        private byte[] _activationSecret;

        /// <summary>
        /// Initializes a new instance of this class using the system TPM.
        /// </summary>
        /// <remarks>
        /// Calls to the TPM library can potentially return a <see cref="TssException"/> or a <see cref="TpmException"/>
        /// if your TPM hardware does not support the relevant API call.
        /// </remarks>
        /// <param name="registrationId">The Device Provisioning Service Registration Id.</param>
        public AuthenticationProviderTpmHsm(string registrationId)
            : this(registrationId, CreateDefaultTpm2Device()) { }

        /// <summary>
        /// Initializes a new instance of this class using the specified TPM module.
        /// </summary>
        /// <remarks>
        /// Calls to the TPM library can potentially return a <see cref="TssException"/> or a <see cref="TpmException"/>
        /// if your TPM hardware does not support the relevant API call.
        /// </remarks>
        /// <param name="registrationId">The Device Provisioning Service Registration Id.</param>
        /// <param name="tpm">The TPM device.</param>
        public AuthenticationProviderTpmHsm(string registrationId, Tpm2Device tpm)
            : base(registrationId)
        {
            _tpmDevice = tpm ?? throw new ArgumentNullException(nameof(tpm));

            _tpmDevice.Connect();
            _tpm2 = new Tpm2(_tpmDevice);
            CacheEkAndSrk();

            if (Logging.IsEnabled)
            {
                Logging.Associate(this, _tpm2);
                Logging.Associate(_tpm2, _tpmDevice);

#if DEBUG
                _tpm2._SetTraceCallback((byte[] inBuffer, byte[] outBuffer) =>
                {
                    if (Logging.IsEnabled)
                    {
                        Logging.Info(this, $"TPM data: {Logging.GetHashCode(_tpm2)}");
                        Logging.DumpBuffer(_tpm2, inBuffer, nameof(inBuffer));
                        Logging.DumpBuffer(_tpm2, outBuffer, nameof(outBuffer));
                    }
                });
#endif
            }
        }

        private static Tpm2Device CreateDefaultTpm2Device()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (Logging.IsEnabled)
                    Logging.Info(null, "Creating Windows TBS Device.");

                return new TbsDevice();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (Logging.IsEnabled)
                    Logging.Info(null, "Creating Linux /dev/tpm0 Device.");

                return new LinuxTpmDevice();
            }

            if (Logging.IsEnabled)
                Logging.Error(null, $"TPM not supported on {RuntimeInformation.OSDescription}");

            throw new PlatformNotSupportedException("The library doesn't support the current OS platform.");
        }

        /// <summary>
        /// Activates an identity key within the TPM device.
        /// </summary>
        /// <remarks>
        /// Calls to the TPM library can potentially return a <see cref="TssException"/> or a <see cref="TpmException"/>
        /// if your TPM hardware does not support the relevant API call.
        /// </remarks>
        /// <param name="encryptedKey">The encrypted identity key.</param>
        public override void ActivateIdentityKey(byte[] encryptedKey)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{encryptedKey}", nameof(ActivateIdentityKey));

            Destroy();

            // Take the pieces out of the container
            var marshaller = new Marshaller(encryptedKey, DataRepresentation.Tpm);
            Tpm2bIdObject cred2b = marshaller.Get<Tpm2bIdObject>();
            byte[] encryptedSecret = new byte[marshaller.Get<ushort>()];
            encryptedSecret = marshaller.GetArray<byte>(encryptedSecret.Length, "encryptedSecret");
            TpmPrivate dupBlob = marshaller.Get<TpmPrivate>();
            byte[] encWrapKey = new byte[marshaller.Get<ushort>()];
            encWrapKey = marshaller.GetArray<byte>(encWrapKey.Length, "encWrapKey");
            ushort pubSize = marshaller.Get<ushort>();
            _idKeyPub = marshaller.Get<TpmPublic>();
            byte[] cipherText = new byte[marshaller.Get<ushort>()];
            cipherText = marshaller.GetArray<byte>(cipherText.Length, "uriInfo");

            // Setup the authorization session for the EK
            var policyNode = new TpmPolicySecret(TpmHandle.RhEndorsement, false, 0, Array.Empty<byte>(), Array.Empty<byte>());
            var policy = new PolicyTree(_ekPub.nameAlg);
            policy.SetPolicyRoot(policyNode);
            AuthSession ekSession = _tpm2.StartAuthSessionEx(TpmSe.Policy, _ekPub.nameAlg);
            ekSession.RunPolicy(_tpm2, policy);

            // Perform the activation
            ekSession.Attrs &= ~SessionAttr.ContinueSession;
            _activationSecret = _tpm2[Array.Empty<byte>(), ekSession].ActivateCredential(
                new TpmHandle(Tpm20SrkHandle),
                new TpmHandle(Tpm20EkHandle),
                cred2b.credential,
                encryptedSecret);

            TpmPrivate importedKeyBlob = _tpm2.Import(
                new TpmHandle(Tpm20SrkHandle),
                _activationSecret,
                _idKeyPub,
                dupBlob,
                encWrapKey,
                new SymDefObject(TpmAlgId.Aes, 128, TpmAlgId.Cfb));

            _idKeyHandle = _tpm2.Load(new TpmHandle(Tpm20SrkHandle), importedKeyBlob, _idKeyPub);

            // Persist the key in NV
            var hmacKeyHandle = new TpmHandle(AiothPersistedKeyHandle);
            _tpm2.EvictControl(new TpmHandle(TpmRh.Owner), _idKeyHandle, hmacKeyHandle);

            // Unload the transient copy from the TPM
            _tpm2.FlushContext(_idKeyHandle);
            _idKeyHandle = hmacKeyHandle;

            if (Logging.IsEnabled)
                Logging.Exit(this, $"{encryptedKey}", nameof(ActivateIdentityKey));
        }

        /// <summary>
        /// Gets the Base64 encoded EndorsmentKey.
        /// </summary>
        /// <remarks>
        /// Calls to the TPM library can potentially return a <see cref="TssException"/> or a <see cref="TpmException"/>
        /// if your TPM hardware does not support the relevant API call.
        /// </remarks>
        /// <returns>Base64 encoded EK.</returns>
        public override byte[] GetEndorsementKey()
        {
            byte[] ek = _ekPub.GetTpm2BRepresentation();

            if (Logging.IsEnabled)
                Logging.Info(this, $"EK={Convert.ToBase64String(ek)}");

            return ek;
        }

        /// <summary>
        /// Gets the Base64 encoded StorageRootKey.
        /// </summary>
        /// <remarks>
        /// Calls to the TPM library can potentially return a <see cref="TssException"/> or a <see cref="TpmException"/>
        /// if your TPM hardware does not support the relevant API call.
        /// </remarks>
        /// <returns>Base64 encoded SRK.</returns>
        public override byte[] GetStorageRootKey()
        {
            byte[] srk = _srkPub.GetTpm2BRepresentation();

            if (Logging.IsEnabled)
                Logging.Info(this, $"SRK={Convert.ToBase64String(srk)}");

            return srk;
        }

        /// <summary>
        /// Signs the data using the previously activated identity key.
        /// </summary>
        /// <remarks>
        /// Calls to the TPM library can potentially return a <see cref="TssException"/> or a <see cref="TpmException"/>
        /// if your TPM hardware does not support the relevant API call.
        /// </remarks>
        /// <param name="data">The data to be signed.</param>
        /// <returns>The signed data.</returns>
        public override byte[] Sign(byte[] data)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, null, nameof(Sign));

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            byte[] result = Array.Empty<byte>();
            var hmacKeyHandle = new TpmHandle(AiothPersistedKeyHandle);
            int dataIndex = 0;
            byte[] iterationBuffer;

            const int maxBufferLength = 1024;

            if (data.Length <= maxBufferLength)
            {
                result = _tpm2.Hmac(hmacKeyHandle, data, TpmAlgId.Sha256);
            }
            else
            {
                // Start the HMAC sequence.
                TpmHandle hmacHandle = _tpm2.HmacStart(hmacKeyHandle, Array.Empty<byte>(), TpmAlgId.Sha256);
                while (data.Length > dataIndex + maxBufferLength)
                {
                    // Repeat to update the HMAC until we only have <=1024 bytes left.
                    iterationBuffer = new byte[maxBufferLength];
                    Array.Copy(data, dataIndex, iterationBuffer, 0, maxBufferLength);
                    _tpm2.SequenceUpdate(hmacHandle, iterationBuffer);
                    dataIndex += maxBufferLength;
                }

                // Finalize the HMAC with the remainder of the data.
                iterationBuffer = new byte[data.Length - dataIndex];
                Array.Copy(data, dataIndex, iterationBuffer, 0, data.Length - dataIndex);
                result = _tpm2.SequenceComplete(hmacHandle, iterationBuffer, TpmHandle.RhNull, out TkHashcheck nullChk);
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, null, nameof(Sign));

            return result;
        }

        /// <summary>
        /// Releases the unmanaged resources used by this class and optionally disposes of the managed resources.
        /// </summary>
        /// <remarks>
        /// Calls to the TPM library can potentially return a <see cref="TssException"/> or a <see cref="TpmException"/>
        /// if your TPM hardware does not support the relevant API call.
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (Logging.IsEnabled)
                Logging.Info(this, "Disposing");

            // _tpmDevice is owned by _tpm2, which will disposed it, but not if it is null.
            if (_tpm2 == null)
            {
                _tpmDevice?.Dispose();
                _tpmDevice = null;
            }

            _tpm2.Dispose();
            _tpm2 = null;

            _disposed = true;
        }

        private void Destroy()
        {
            var nvHandle = new TpmHandle(AiothPersistedUriIindex);
            var ownerHandle = new TpmHandle(TpmRh.Owner);
            var hmacKeyHandle = new TpmHandle(AiothPersistedKeyHandle);

            try
            {
                // Destroy the URI
                _tpm2.NvUndefineSpace(ownerHandle, nvHandle);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                // ignore
            }

            try
            {
                // Destroy the HMAC key
                _tpm2.EvictControl(ownerHandle, hmacKeyHandle, hmacKeyHandle);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                // ignore
            }
        }

        private void CacheEkAndSrk()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, null, nameof(CacheEkAndSrk));

            // Get the real EK ready.
            var ekTemplate = new TpmPublic(
                TpmAlgId.Sha256,
                ObjectAttr.FixedTPM | ObjectAttr.FixedParent | ObjectAttr.SensitiveDataOrigin |
                ObjectAttr.AdminWithPolicy | ObjectAttr.Restricted | ObjectAttr.Decrypt,
                new byte[]
                {
                    0x83, 0x71, 0x97, 0x67, 0x44, 0x84, 0xb3, 0xf8, 0x1a, 0x90, 0xcc, 0x8d, 0x46, 0xa5, 0xd7, 0x24,
                    0xfd, 0x52, 0xd7, 0x6e, 0x06, 0x52, 0x0b, 0x64, 0xf2, 0xa1, 0xda, 0x1b, 0x33, 0x14, 0x69, 0xaa
                },
                new RsaParms(
                    new SymDefObject(TpmAlgId.Aes, 128, TpmAlgId.Cfb),
                    new NullAsymScheme(),
                    2048,
                    0),
                new Tpm2bPublicKeyRsa(new byte[2048 / 8]));

            _ekPub = ReadOrCreatePersistedKey(new TpmHandle(Tpm20EkHandle), new TpmHandle(TpmHandle.RhEndorsement), ekTemplate);

            // Get the real SRK ready.
            var srkTemplate = new TpmPublic(
                TpmAlgId.Sha256,
                ObjectAttr.FixedTPM | ObjectAttr.FixedParent | ObjectAttr.SensitiveDataOrigin |
                    ObjectAttr.UserWithAuth | ObjectAttr.NoDA | ObjectAttr.Restricted | ObjectAttr.Decrypt,
                Array.Empty<byte>(),
                new RsaParms(
                    new SymDefObject(TpmAlgId.Aes, 128, TpmAlgId.Cfb),
                    new NullAsymScheme(),
                    2048,
                    0),
                    new Tpm2bPublicKeyRsa(new byte[2048 / 8]));

            _srkPub = ReadOrCreatePersistedKey(new TpmHandle(Tpm20SrkHandle), new TpmHandle(TpmHandle.RhOwner), srkTemplate);

            if (Logging.IsEnabled)
                Logging.Exit(this, null, nameof(CacheEkAndSrk));
        }

        private TpmPublic ReadOrCreatePersistedKey(TpmHandle persHandle, TpmHandle hierarchy, TpmPublic template)
        {
            // Let's see if the key was already created and installed.
            TpmPublic keyPub = _tpm2._AllowErrors().ReadPublic(persHandle, out _, out _);

            // If not create and install it.
            if (!_tpm2._LastCommandSucceeded())
            {
                TpmHandle keyHandle = _tpm2.CreatePrimary(hierarchy,
                    new SensitiveCreate(),
                    template,
                    Array.Empty<byte>(),
                    Array.Empty<PcrSelection>(),
                    out keyPub,
                    out _,
                    out _,
                    creationTicket: out _);
                _tpm2.EvictControl(TpmHandle.RhOwner, keyHandle, persHandle);
                _tpm2.FlushContext(keyHandle);
            }

            return keyPub;
        }
    }
}
