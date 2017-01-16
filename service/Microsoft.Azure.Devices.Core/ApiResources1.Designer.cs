﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.Azure.Devices.Core {
    using System;
    using System.Reflection;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class ApiResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal ApiResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.Azure.Devices.Core.ApiResources", typeof(ApiResources).GetTypeInfo().Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The value of this argument must be non-negative..
        /// </summary>
        internal static string ArgumentMustBeNonNegative {
            get {
                return ResourceManager.GetString("ArgumentMustBeNonNegative", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The value of this argument must be positive..
        /// </summary>
        internal static string ArgumentMustBePositive {
            get {
                return ResourceManager.GetString("ArgumentMustBePositive", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The connection string is not well formed..
        /// </summary>
        internal static string ConnectionStringIsNotWellFormed {
            get {
                return ResourceManager.GetString("ConnectionStringIsNotWellFormed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Device {0} cannot specify both symmetric keys and thumbprints..
        /// </summary>
        internal static string DeviceAuthenticationInvalid {
            get {
                return ResourceManager.GetString("DeviceAuthenticationInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The device identifier {0} is invalid..
        /// </summary>
        internal static string DeviceIdInvalid {
            get {
                return ResourceManager.GetString("DeviceIdInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The id of the device was not set..
        /// </summary>
        internal static string DeviceIdNotSet {
            get {
                return ResourceManager.GetString("DeviceIdNotSet", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to null or empty deviceIds.
        /// </summary>
        internal static string DeviceJobParametersNullOrEmptyDeviceList {
            get {
                return ResourceManager.GetString("DeviceJobParametersNullOrEmptyDeviceList", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to null or empty deviceId entries specified.
        /// </summary>
        internal static string DeviceJobParametersNullOrEmptyDeviceListEntries {
            get {
                return ResourceManager.GetString("DeviceJobParametersNullOrEmptyDeviceListEntries", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Either both primary and secondary keys must be specified or neither one to auto generate on service side..
        /// </summary>
        internal static string DeviceKeysInvalid {
            get {
                return ResourceManager.GetString("DeviceKeysInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The ETag should be set while deleting the device..
        /// </summary>
        internal static string ETagNotSetWhileDeletingDevice {
            get {
                return ResourceManager.GetString("ETagNotSetWhileDeletingDevice", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The ETag should be set while updating the device..
        /// </summary>
        internal static string ETagNotSetWhileUpdatingDevice {
            get {
                return ResourceManager.GetString("ETagNotSetWhileUpdatingDevice", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The ETag should not be set while registering the device..
        /// </summary>
        internal static string ETagSetWhileRegisteringDevice {
            get {
                return ResourceManager.GetString("ETagSetWhileRegisteringDevice", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Serialization operation failed due to unsupported type {0}..
        /// </summary>
        internal static string FailedToSerializeUnsupportedType {
            get {
                return ResourceManager.GetString("FailedToSerializeUnsupportedType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The HostName is null..
        /// </summary>
        internal static string HostNameIsNull {
            get {
                return ResourceManager.GetString("HostNameIsNull", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The endpoint in the connection string is invalid..
        /// </summary>
        internal static string InvalidConnectionStringEndpoint {
            get {
                return ResourceManager.GetString("InvalidConnectionStringEndpoint", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The password is not valid..
        /// </summary>
        internal static string InvalidPassword {
            get {
                return ResourceManager.GetString("InvalidPassword", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The connection string has an invalid value for property: {0}.
        /// </summary>
        internal static string InvalidPropertyInConnectionString {
            get {
                return ResourceManager.GetString("InvalidPropertyInConnectionString", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The User is not valid..
        /// </summary>
        internal static string InvalidUser {
            get {
                return ResourceManager.GetString("InvalidUser", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The JobClient instance was already closed..
        /// </summary>
        internal static string JobClientInstanceAlreadyClosed {
            get {
                return ResourceManager.GetString("JobClientInstanceAlreadyClosed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The message body cannot be read multiple times. To reuse it store the value after reading..
        /// </summary>
        internal static string MessageBodyConsumed {
            get {
                return ResourceManager.GetString("MessageBodyConsumed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This messaging entity has already been closed, aborted, or disposed..
        /// </summary>
        internal static string MessageDisposed {
            get {
                return ResourceManager.GetString("MessageDisposed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The connection string is missing the property: {0}.
        /// </summary>
        internal static string MissingPropertyInConnectionString {
            get {
                return ResourceManager.GetString("MissingPropertyInConnectionString", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified offset exceeds the buffer size ({0} bytes)..
        /// </summary>
        internal static string OffsetExceedsBufferSize {
            get {
                return ResourceManager.GetString("OffsetExceedsBufferSize", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The parameter {0} cannot be null or empty..
        /// </summary>
        internal static string ParameterCannotBeNullOrEmpty {
            get {
                return ResourceManager.GetString("ParameterCannotBeNullOrEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The parameter {0} cannot be null, empty or whitespace..
        /// </summary>
        internal static string ParameterCannotBeNullOrWhitespace {
            get {
                return ResourceManager.GetString("ParameterCannotBeNullOrWhitespace", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The {0} property is not a valid Uri.
        /// </summary>
        internal static string PropertyIsNotValidUri {
            get {
                return ResourceManager.GetString("PropertyIsNotValidUri", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The RegistryManager instance was already closed..
        /// </summary>
        internal static string RegistryManagerInstanceAlreadyClosed {
            get {
                return ResourceManager.GetString("RegistryManagerInstanceAlreadyClosed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified size exceeds the remaining buffer space ({0} bytes)..
        /// </summary>
        internal static string SizeExceedsRemainingBufferSpace {
            get {
                return ResourceManager.GetString("SizeExceedsRemainingBufferSpace", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; is not a valid X.509 thumbprint.
        /// </summary>
        internal static string StringIsNotThumbprint {
            get {
                return ResourceManager.GetString("StringIsNotThumbprint", resourceCulture);
            }
        }
    }
}
