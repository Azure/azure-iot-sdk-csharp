// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Azure.Iot.DigitalTwin.Device.Model
{
    public class Callbacks
    {
        public delegate Task PropertyUpdatedCallback(DigitalTwinPropertyUpdate propertyUpdate, object userContext);

        public delegate Task<DigitalTwinCommandResponse> CommandCallback(DigitalTwinCommandRequest commandRequest, object userContext);

        internal PropertyUpdatedCallback PropertyUpdatedCB { get; private set; }

        internal CommandCallback CommandCB { get; private set; }

        public Callbacks(PropertyUpdatedCallback propertyUpdatedCallback, CommandCallback commandCallback)
        {
            this.CommandCB = commandCallback;
            this.PropertyUpdatedCB = propertyUpdatedCallback;
        }
    }
}
