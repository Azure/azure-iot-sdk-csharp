// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Runtime.InteropServices;
using System.Text;
using Tpm2Lib;

namespace Microsoft.Azure.Devices.Provisioning.Security
{
    /// <summary>
    /// The Provisioning Security Client implementation for TPM.
    /// </summary>
    public class SecurityProviderTpmHsm : SecurityProviderTpm
    {
        private bool disposed = false;

        private const uint TPM_20_SRK_HANDLE = ((uint)Ht.Persistent << 24) | 0x00000001;
        private const uint TPM_20_EK_HANDLE = ((uint)Ht.Persistent << 24) | 0x00010001;
        private const uint AIOTH_PERSISTED_URI_INDEX = ((uint)Ht.NvIndex << 24) | 0x00400100;
        private const uint AIOTH_PERSISTED_KEY_HANDLE = ((uint)Ht.Persistent << 24) | 0x00000100;

        private Tpm2Device _tpmDevice = null;
        private Tpm2 _tpm2;

        // TPM identity cache
        private TpmPublic _ekPub = null;
        private TpmPublic _srkPub = null;
        private TpmPublic _idKeyPub = null;
        private TpmHandle _idKeyHandle = TpmHandle.RhNull;
        private byte[] _activationSecret = null;

        /// <summary>
        /// Initializes a new instance of the SecurityProviderTpmHsm class using the system TPM.
        /// </summary>
        /// <param name="registrationId">The Device Provisioning Service Registration ID.</param>
        public SecurityProviderTpmHsm(string registrationId) : this(registrationId, CreateDefaultTpm2Device()) { }

        /// <summary>
        /// Initializes a new instance of the SecurityProviderTpmHsm class using the specified TPM module.
        /// </summary>
        /// <param name="registrationId">The Device Provisioning Service Registration ID.</param>
        /// <param name="tpm">The TPM device.</param>
        public SecurityProviderTpmHsm(string registrationId, Tpm2Device tpm) : base(registrationId)
        {
            _tpmDevice = tpm;

            _tpmDevice.Connect();
            _tpm2 = new Tpm2(_tpmDevice);
            CacheEkAndSrk();

            if (Logging.IsEnabled)
            {
                Logging.Associate(this, _tpm2);
                Logging.Associate(_tpm2, _tpmDevice);

                _tpm2._SetTraceCallback((byte[] inBuffer, byte[] outBuffer) =>
                {
                    if (Logging.IsEnabled)
                    {
                        Logging.Info(this, $"TPM data: {Logging.GetHashCode(_tpm2)}");
                        Logging.DumpBuffer(_tpm2, inBuffer, nameof(inBuffer));
                        Logging.DumpBuffer(_tpm2, outBuffer, nameof(outBuffer));
                    }
                });
            }
        }

        private static Tpm2Device CreateDefaultTpm2Device()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (Logging.IsEnabled) Logging.Info(null, "Creating Windows TBS Device.");
                return new TbsDevice();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (Logging.IsEnabled) Logging.Info(null, "Creating Linux /dev/tpm0 Device.");
                return new LinuxTpmDevice();
            }
            else
            {
                if (Logging.IsEnabled) Logging.Error(null, $"TPM not supported on {RuntimeInformation.OSDescription}");
                throw new PlatformNotSupportedException("The library doesn't support the current OS platform.");
            }
        }
        
        /// <summary>
        /// Activates an identity key within the TPM device.
        /// </summary>
        /// <param name="encryptedKey">The encrypted identity key.</param>
        public override void ActivateIdentityKey(byte[] encryptedKey)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{encryptedKey}", nameof(ActivateIdentityKey));
            Destroy();

            // Take the pieces out of the container
            var m = new Marshaller(encryptedKey, DataRepresentation.Tpm);
            Tpm2bIdObject cred2b = m.Get<Tpm2bIdObject>();
            byte[] encryptedSecret = new byte[m.Get<ushort>()];
            encryptedSecret = m.GetArray<byte>(encryptedSecret.Length, "encryptedSecret");
            TpmPrivate dupBlob = m.Get<TpmPrivate>();
            byte[] encWrapKey = new byte[m.Get<ushort>()];
            encWrapKey = m.GetArray<byte>(encryptedSecret.Length, "encWrapKey");
            UInt16 pubSize = m.Get<UInt16>();
            _idKeyPub = m.Get<TpmPublic>();
            byte[] cipherText = new byte[m.Get<ushort>()];
            cipherText = m.GetArray<byte>(cipherText.Length, "uriInfo");

            // Setup the authorization session for the EK
            var policyNode = new TpmPolicySecret(TpmHandle.RhEndorsement, false, 0, Array.Empty<byte>(), Array.Empty<byte>());
            var policy = new PolicyTree(_ekPub.nameAlg);
            policy.SetPolicyRoot(policyNode);
            AuthSession ekSession = _tpm2.StartAuthSessionEx(TpmSe.Policy, _ekPub.nameAlg);
            ekSession.RunPolicy(_tpm2, policy);

            // Perform the activation
            ekSession.Attrs &= ~SessionAttr.ContinueSession;
            _activationSecret = _tpm2[Array.Empty<byte>(), ekSession].ActivateCredential(
                new TpmHandle(TPM_20_SRK_HANDLE), 
                new TpmHandle(TPM_20_EK_HANDLE), 
                cred2b.credential, 
                encryptedSecret);

            TpmPrivate importedKeyBlob = _tpm2.Import(
                new TpmHandle(TPM_20_SRK_HANDLE), 
                _activationSecret, 
                _idKeyPub, 
                dupBlob, 
                encWrapKey, 
                new SymDefObject(TpmAlgId.Aes, 128, TpmAlgId.Cfb));

            _idKeyHandle = _tpm2.Load(new TpmHandle(TPM_20_SRK_HANDLE), importedKeyBlob, _idKeyPub);

            // Persist the key in NV
            TpmHandle hmacKeyHandle = new TpmHandle(AIOTH_PERSISTED_KEY_HANDLE);
            _tpm2.EvictControl(new TpmHandle(TpmRh.Owner), _idKeyHandle, hmacKeyHandle);

            // Unload the transient copy from the TPM
            _tpm2.FlushContext(_idKeyHandle);
            _idKeyHandle = hmacKeyHandle;

            if (Logging.IsEnabled) Logging.Exit(this, $"{encryptedKey}", nameof(ActivateIdentityKey));
        }

        /// <summary>
        /// Gets the Base64 encoded EndorsmentKey.
        /// </summary>
        /// <returns>Base64 encoded EK.</returns>
        public override byte[] GetEndorsementKey()
        {
            byte [] ek = _ekPub.GetTpm2BRepresentation();
            if (Logging.IsEnabled) Logging.Info(this, $"EK={Convert.ToBase64String(ek)}");
            return ek;
        }

        /// <summary>
        /// Gets the Base64 encoded StorageRootKey.
        /// </summary>
        /// <returns>Base64 encoded SRK.</returns>
        public override byte[] GetStorageRootKey()
        {
            byte[] srk = _srkPub.GetTpm2BRepresentation();
            if (Logging.IsEnabled) Logging.Info(this, $"SRK={Convert.ToBase64String(srk)}");
            return srk;
        }

        /// <summary>
        /// Signs the data using the previously activated identity key.
        /// </summary>
        /// <param name="data">The data to be signed.</param>
        /// <returns>The signed data.</returns>
        public override byte[] Sign(byte[] data)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{Convert.ToBase64String(data)}", nameof(Sign));

            byte[] result = Array.Empty<byte>();
            TpmHandle hmacKeyHandle = new TpmHandle(AIOTH_PERSISTED_KEY_HANDLE);
            int dataIndex = 0;
            byte[] iterationBuffer;

            if (data.Length <= 1024)
            {
                result = _tpm2.Hmac(hmacKeyHandle, data, TpmAlgId.Sha256);
            }
            else
            {
                // Start the HMAC sequence.
                TpmHandle hmacHandle = _tpm2.HmacStart(hmacKeyHandle, Array.Empty<byte>(), TpmAlgId.Sha256);
                while (data.Length > dataIndex + 1024)
                {
                    // Repeat to update the HMAC until we only have <=1024 bytes left.
                    iterationBuffer = new Byte[1024];
                    Array.Copy(data, dataIndex, iterationBuffer, 0, 1024);
                    _tpm2.SequenceUpdate(hmacHandle, iterationBuffer);
                    dataIndex += 1024;
                }

                // Finalize the HMAC with the remainder of the data.
                iterationBuffer = new Byte[data.Length - dataIndex];
                Array.Copy(data, dataIndex, iterationBuffer, 0, data.Length - dataIndex);
                result = _tpm2.SequenceComplete(hmacHandle, iterationBuffer, TpmHandle.RhNull, out TkHashcheck nullChk);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{Convert.ToBase64String(result)}", nameof(Sign));
            return result;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the SecurityProviderTpmHsm and optionally disposes of the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to releases only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposed) return;

            if (Logging.IsEnabled) Logging.Info(this, "Disposing");

            if (disposing)
            {
                _tpm2?.Dispose();
                _tpm2 = null;
                _tpmDevice = null;
            }

            disposed = true;
        }
        
        private void Destroy()
        {
            TpmHandle nvHandle = new TpmHandle(AIOTH_PERSISTED_URI_INDEX);
            TpmHandle ownerHandle = new TpmHandle(TpmRh.Owner);
            TpmHandle hmacKeyHandle = new TpmHandle(AIOTH_PERSISTED_KEY_HANDLE);

            try
            {
                // Destroy the URI
                _tpm2.NvUndefineSpace(ownerHandle, nvHandle);
            }
            catch
            {
                // ignore 
            }

            try
            {
                // Destroy the HMAC key
                _tpm2.EvictControl(ownerHandle, hmacKeyHandle, hmacKeyHandle);
            }
            catch
            { 
                // ignore 
            }
        }

        private void CacheEkAndSrk()
        {
            if (Logging.IsEnabled) Logging.Enter(this, null, nameof(CacheEkAndSrk));

            // Get the real EK ready.
            TpmPublic ekTemplate = new TpmPublic(
                TpmAlgId.Sha256,
                ObjectAttr.FixedTPM | ObjectAttr.FixedParent | ObjectAttr.SensitiveDataOrigin | 
                ObjectAttr.AdminWithPolicy | ObjectAttr.Restricted | ObjectAttr.Decrypt,
                new byte[] {
                    0x83, 0x71, 0x97, 0x67, 0x44, 0x84, 0xb3, 0xf8, 0x1a, 0x90, 0xcc, 0x8d, 0x46, 0xa5, 0xd7, 0x24,
                    0xfd, 0x52, 0xd7, 0x6e, 0x06, 0x52, 0x0b, 0x64, 0xf2, 0xa1, 0xda, 0x1b, 0x33, 0x14, 0x69, 0xaa },
                new RsaParms(
                    new SymDefObject(TpmAlgId.Aes, 128, TpmAlgId.Cfb),
                    new NullAsymScheme(),
                    2048,
                    0),
                new Tpm2bPublicKeyRsa(new Byte[2048 / 8]));

            _ekPub = ReadOrCreatePersistedKey(new TpmHandle(TPM_20_EK_HANDLE), new TpmHandle(TpmHandle.RhEndorsement), ekTemplate);

            // Get the real SRK ready.
            TpmPublic srkTemplate = new TpmPublic(
                TpmAlgId.Sha256,
                ObjectAttr.FixedTPM | ObjectAttr.FixedParent | ObjectAttr.SensitiveDataOrigin | 
                ObjectAttr.UserWithAuth | ObjectAttr.NoDA | ObjectAttr.Restricted | ObjectAttr.Decrypt,
                Array.Empty<byte>(),
                new RsaParms(
                    new SymDefObject(TpmAlgId.Aes, 128, TpmAlgId.Cfb),
                    new NullAsymScheme(),
                    2048,
                    0),
                    new Tpm2bPublicKeyRsa(new Byte[2048 / 8]));

            _srkPub = ReadOrCreatePersistedKey(new TpmHandle(TPM_20_SRK_HANDLE), new TpmHandle(TpmHandle.RhOwner), srkTemplate);

            if (Logging.IsEnabled) Logging.Exit(this, null, nameof(CacheEkAndSrk));
        }

        private TpmPublic ReadOrCreatePersistedKey(TpmHandle persHandle, TpmHandle hierarchy, TpmPublic template)
        {
            byte[] name;
            byte[] qualifiedName;

            // Let's see if the key was already created and installed.
            TpmPublic keyPub = _tpm2._AllowErrors().ReadPublic(persHandle, out name, out qualifiedName);

            // If not create and install it.
            if (!_tpm2._LastCommandSucceeded())
            {
                CreationData creationData;
                byte[] creationHash;
                TkCreation creationTicket;
                TpmHandle keyHandle = _tpm2.CreatePrimary(hierarchy,
                    new SensitiveCreate(),
                    template,
                    Array.Empty<byte>(),
                    Array.Empty<PcrSelection>(),
                    out keyPub,
                    out creationData,
                    out creationHash,
                    out creationTicket);
                _tpm2.EvictControl(TpmHandle.RhOwner, keyHandle, persHandle);
                _tpm2.FlushContext(keyHandle);
            }
            return keyPub;
        }
    }
}
