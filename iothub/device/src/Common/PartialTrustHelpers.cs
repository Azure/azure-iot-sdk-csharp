// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
#if NET451
using System.Security.Permissions;
#endif

namespace Microsoft.Azure.Devices.Client
{

    internal static class PartialTrustHelpers
    {
#if NET451
        [Fx.Tag.SecurityNote(Critical = "used in a security-sensitive decision")]
        [SecurityCritical]
        private static Type s_aptca;
#endif
        internal static bool ShouldFlowSecurityContext
        {
            [Fx.Tag.SecurityNote(Critical = "used in a security-sensitive decision")]
            [SecurityCritical]
            get
            {
#if !NET451
                throw new NotImplementedException();
#else
                return AppDomain.CurrentDomain.IsHomogenous
                    ? false
                    : SecurityManager.CurrentThreadRequiresSecurityContextCapture();
#endif
            }
        }

        [Fx.Tag.SecurityNote(Critical = "used in a security-sensitive decision")]
        [SecurityCritical]
        internal static bool IsInFullTrust()
        {
#if !NET451
            throw new NotImplementedException();
#else
            if (AppDomain.CurrentDomain.IsHomogenous)
            {
                return AppDomain.CurrentDomain.IsFullyTrusted;
            }
            else
            {
                if (!SecurityManager.CurrentThreadRequiresSecurityContextCapture())
                {
                    return true;
                }

                try
                {
                    DemandForFullTrust();
                    return true;
                }
                catch (SecurityException)
                {
                    return false;
                }
            }
#endif
        }

        [Fx.Tag.SecurityNote(Critical = "used in a security-sensitive decision")]
        [SecurityCritical]
        internal static bool UnsafeIsInFullTrust()
        {
#if !NET451
            throw new NotImplementedException();
#else
            if (AppDomain.CurrentDomain.IsHomogenous)
            {
                return AppDomain.CurrentDomain.IsFullyTrusted;
            }
            else
            {
                return !SecurityManager.CurrentThreadRequiresSecurityContextCapture();
            }
#endif
        }

#if NET451
        [Fx.Tag.SecurityNote(Critical = "Captures security context with identity flow suppressed, " +
            "this requires satisfying a LinkDemand for infrastructure.")]
        [SecurityCritical]
        internal static SecurityContext CaptureSecurityContextNoIdentityFlow()
        {
            // capture the security context but never flow windows identity
            if (SecurityContext.IsWindowsIdentityFlowSuppressed())
            {
                return SecurityContext.Capture();
            }
            else
            {
                using (SecurityContext.SuppressFlowWindowsIdentity())
                {
                    return SecurityContext.Capture();
                }
            }
        }
#endif
        [Fx.Tag.SecurityNote(Critical = "used in a security-sensitive decision")]
        [SecurityCritical]
        internal static bool IsTypeAptca(Type type)
        {
#if !NET451
            throw new NotImplementedException();
#else
            Assembly assembly = type.Assembly;
            return IsAssemblyAptca(assembly) || !IsAssemblySigned(assembly);
#endif
        }

        [Fx.Tag.SecurityNote(Critical = "used in a security-sensitive decision")]
        [SecurityCritical]
#if NET451
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
#endif
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DemandForFullTrust()
        {
        }

        [Fx.Tag.SecurityNote(Critical = "used in a security-sensitive decision")]
        [SecurityCritical]
        private static bool IsAssemblyAptca(Assembly assembly)
        {
#if !NET451
            throw new NotImplementedException();
#else
            if (s_aptca == null)
            {
                s_aptca = typeof(AllowPartiallyTrustedCallersAttribute);
            }
            return assembly.GetCustomAttributes(s_aptca, false).Length > 0;
#endif
        }

        [Fx.Tag.SecurityNote(Critical = "used in a security-sensitive decision")]
        [SecurityCritical]
#if NET451
        [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
#endif
        private static bool IsAssemblySigned(Assembly assembly)
        {
            byte[] publicKeyToken = assembly.GetName().GetPublicKeyToken();
            return publicKeyToken != null & publicKeyToken.Length > 0;
        }

#if NET451
        [Fx.Tag.SecurityNote(Critical = "used in a security-sensitive decision")]
        [SecurityCritical]
        internal static bool CheckAppDomainPermissions(PermissionSet permissions)
        {

            return AppDomain.CurrentDomain.IsHomogenous &&
                permissions.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet);
        }
#endif

        [Fx.Tag.SecurityNote(Critical = "used in a security-sensitive decision")]
        [SecurityCritical]
        internal static bool HasEtwPermissions()
        {
#if !NET451
            throw new NotImplementedException();
#else
            // Currently unrestricted permissions are required to create Etw provider. 
            var permissions = new PermissionSet(PermissionState.Unrestricted);
            return CheckAppDomainPermissions(permissions);
#endif
        }
    }
}
