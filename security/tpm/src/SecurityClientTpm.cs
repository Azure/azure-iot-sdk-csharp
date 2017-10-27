// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Diagnostics;
using System.Text;
using Tpm2Lib;

namespace Microsoft.Azure.Devices.Provisioning.Security
{
    /// <summary>
    /// The Provisioning Security Client implementation for TPM.
    /// </summary>
    public class SecurityClientTpm : SecurityClientHsmTpm
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
        private byte[] _ekAuth = Array.Empty<Byte>();
        private TpmPublic _idKeyPub = null;
        private TpmHandle _idKeyHandle = TpmHandle.RhNull;
        private byte[] _activationSecret = null;

        /// <summary>
        /// Constructor creating an instance using the system TPM.
        /// </summary>
        /// <param name="registrationId">The Device Provisioning Service Registration ID.</param>
        public SecurityClientTpm(string registrationId) : this(registrationId, CreateDefaultTpm2Device()) { }

        /// <summary>
        /// Constructor creating an instance using the specified TPM module.
        /// </summary>
        /// <param name="registrationId">The Device Provisioning Service Registration ID.</param>
        /// <param name="tpm">The TPM device.</param>
        public SecurityClientTpm(string registrationId, Tpm2Device tpm) : base(registrationId)
        {
            _tpmDevice = tpm;

            _tpmDevice.Connect();
            _tpm2 = new Tpm2(_tpmDevice);
            CacheEkAndSrk();
        }

        private static Tpm2Device CreateDefaultTpm2Device()
        {
            // TODO: Add LinuxTpmDevice support.
            return new TbsDevice();
        }

        /// <summary>
        /// Activates a symmetric identity within the Hardware Security Module.
        /// </summary>
        /// <param name="activation">The authentication challenge key supplied by the service.</param>
        public override void ActivateSymmetricIdentity(byte[] activation)
        {
            Destroy();

            // Take the pieces out of the container
            var m = new Marshaller(activation, DataRepresentation.Tpm);
            byte[] credentialBlob = new byte[m.Get<ushort>()];
            credentialBlob = m.GetArray<byte>(credentialBlob.Length, "credentialBlob");
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
            var policyNode = new TpmPolicySecret(
                TpmHandle.RhEndorsement, _ekAuth ?? Array.Empty<byte>(), 
                new AuthValue(), 
                false, 
                0, 
                Array.Empty<byte>(), 
                Array.Empty<byte>());

            var policy = new PolicyTree(_ekPub.nameAlg);
            policy.SetPolicyRoot(policyNode);
            AuthSession ekSession = _tpm2.StartAuthSessionEx(TpmSe.Policy, _ekPub.nameAlg);
            ekSession.RunPolicy(_tpm2, policy);

            // Perform the activation
            ekSession.Attrs &= ~SessionAttr.ContinueSession;
            _activationSecret = _tpm2[Array.Empty<byte>(), ekSession].ActivateCredential(
                new TpmHandle(TPM_20_SRK_HANDLE), 
                new TpmHandle(TPM_20_EK_HANDLE), 
                credentialBlob, 
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

            // Unwrap the URI
            byte[] clearText = SymmCipher.Decrypt(
                new SymDefObject(TpmAlgId.Aes, 128, TpmAlgId.Cfb),
                _activationSecret,
                new byte[16], 
                cipherText);

            UnicodeEncoding unicode = new UnicodeEncoding();
            string uriData = unicode.GetString(clearText);
            int idx = uriData.IndexOf('/');
            if (idx > 0)
            {
                string hostName = uriData.Substring(0, idx);
                string deviceId = uriData.Substring(idx + 1);

                // Persist the URI
                ProvisionUri(hostName, deviceId);
            }
        }

        /// <summary>
        /// Gets the Base64 encoded EndorsmentKey.
        /// </summary>
        /// <returns>Base64 encoded EK.</returns>
        public override byte[] GetEndorsementKey()
        {
            return _ekPub.GetTpm2BRepresentation();
        }

        /// <summary>
        /// Gets the Base64 encoded StorageRootKey.
        /// </summary>
        /// <returns>Base64 encoded SRK.</returns>
        public override byte[] GetStorageRootKey()
        {
            return _srkPub.GetTpm2BRepresentation();
        }

        /// <summary>
        /// Signs the data using the Hardware Security Module.
        /// </summary>
        /// <param name="data">The data to be signed.</param>
        /// <returns>The signed data.</returns>
        public override byte[] Sign(byte[] data)
        {
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

            return result;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                _tpmDevice.Dispose();
            }

            disposed = true;
        }
        
        private void ProvisionUri(string hostName, string deviceId = "")
        {
            TpmHandle nvHandle = new TpmHandle(AIOTH_PERSISTED_URI_INDEX);
            TpmHandle ownerHandle = new TpmHandle(TpmRh.Owner);
            UTF8Encoding utf8 = new UTF8Encoding();
            byte[] nvData = utf8.GetBytes(hostName + "/" + deviceId);

            // Define the store
            _tpm2.NvDefineSpace(ownerHandle,
                                  Array.Empty<byte>(),
                                  new NvPublic(nvHandle,
                                               TpmAlgId.Sha256,
                                               NvAttr.Authwrite | NvAttr.Authread | NvAttr.NoDa,
                                               Array.Empty<byte>(),
                                               (ushort)nvData.Length));

            // Write the store
            _tpm2.NvWrite(nvHandle, nvHandle, nvData, 0);
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

        // Constructor helpers
        private void CacheEkAndSrk()
        {
            ObjectAttr ekObjectAttributes =
                ObjectAttr.FixedTPM | ObjectAttr.FixedParent | ObjectAttr.SensitiveDataOrigin |
                ObjectAttr.AdminWithPolicy | ObjectAttr.Restricted | ObjectAttr.Decrypt;

            var ekAuthPolicy = new byte[] 
            {
                0x83, 0x71, 0x97, 0x67, 0x44, 0x84, 0xb3, 0xf8,
                0x1a, 0x90, 0xcc, 0x8d, 0x46, 0xa5, 0xd7, 0x24,
                0xfd, 0x52, 0xd7, 0x6e, 0x06, 0x52, 0x0b, 0x64,
                0xf2, 0xa1, 0xda, 0x1b, 0x33, 0x14, 0x69, 0xaa
            };

            // Get the real EK ready
            TpmPublic ekTemplate = new TpmPublic(
                TpmAlgId.Sha256,
                ekObjectAttributes,
                ekAuthPolicy,
                new RsaParms(new SymDefObject(TpmAlgId.Aes, 128, TpmAlgId.Cfb),
                new NullAsymScheme(),
                2048,
                0),
                new Tpm2bPublicKeyRsa(new Byte[2048 / 8]));

            _ekPub = ReadOrCreatePersistedKey(
                new TpmHandle(TPM_20_EK_HANDLE),
                new TpmHandle(TpmHandle.RhEndorsement),
                ekTemplate);

            ObjectAttr srkObjectAttributes =
                ObjectAttr.FixedTPM | ObjectAttr.FixedParent | ObjectAttr.SensitiveDataOrigin |
                ObjectAttr.UserWithAuth | ObjectAttr.NoDA | ObjectAttr.Restricted | ObjectAttr.Decrypt;

            // Get the real SRK ready
            TpmPublic srkTemplate = new TpmPublic(
                TpmAlgId.Sha256,
                srkObjectAttributes,
                Array.Empty<byte>(),
                new RsaParms(new SymDefObject(TpmAlgId.Aes, 128, TpmAlgId.Cfb),
                new NullAsymScheme(),
                2048,
                0),
                new Tpm2bPublicKeyRsa(new Byte[2048 / 8]));

            _srkPub = ReadOrCreatePersistedKey(
                new TpmHandle(TPM_20_SRK_HANDLE), 
                new TpmHandle(TpmHandle.RhOwner), 
                srkTemplate);
        }

        private TpmPublic ReadOrCreatePersistedKey(TpmHandle persHandle, TpmHandle hierarchy, TpmPublic template)
        {
            // Let's see if the key was already created and installed (aka. the TPM has been provisioned by Windows).
            TpmPublic keyPub = _tpm2._AllowErrors().ReadPublic(persHandle, out byte[] name, out byte[] qualifiedName);

            // If not create and install it
            if (!_tpm2._LastCommandSucceeded())
            {
                TpmHandle keyHandle = _tpm2.CreatePrimary(hierarchy,
                    new SensitiveCreate(),
                    template,
                    Array.Empty<byte>(),
                    Array.Empty<PcrSelection>(),
                    out keyPub,
                    out CreationData creationData,
                    out byte[] creationHash,
                    out TkCreation creationTicket);
                _tpm2.EvictControl(TpmHandle.RhOwner, keyHandle, persHandle);
                _tpm2.FlushContext(keyHandle);
            }

            return keyPub;
        }
    }
}
