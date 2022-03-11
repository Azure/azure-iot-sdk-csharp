// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The role assignment properties.
    /// </summary>
    public class RoleAssignmentProperties
    {

        /// <summary>
        ///     The principal id.
        /// </summary>
        //     The principal id.
        [JsonProperty(PropertyName = "principalId", NullValueHandling = NullValueHandling.Ignore)]
        public string PrincipalId { get; set; }

        /// <summary>
        ///     The role definition id. See https://docs.microsoft.com/en-us/azure/role-based-access-control/role-definitions#role-definition
        ///     for more details.
        /// </summary>
        [JsonProperty(PropertyName = "roleDefinitionId", NullValueHandling = NullValueHandling.Ignore)]
        public string RoleDefinitionId { get; set; }

        /// <summary>
        ///     The role assignment scope. The scope is the resource the role assignment applies
        ///     to. For example, to create a role assignment for an topic space, the scope is
        ///     the fully-qualified ID of the topic space such as /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Devices/IotHubs/{hubName}/topicSpaces/{topicSpaceId}.
        /// </summary>
        [JsonProperty(PropertyName = "scope", NullValueHandling = NullValueHandling.Ignore)]
        public string Scope { get; set; }
    }
}