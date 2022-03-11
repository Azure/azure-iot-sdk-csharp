using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The data structure represent the TopicSpaceTypeEnum
    /// </summary>
    public enum TopicSpaceTypeEnum
    {
        /// <summary>
        /// LowFanout
        /// </summary>
        LowFanout = 1,
        /// <summary>
        /// HighFanout
        /// </summary>
        HighFanout = 2,
        /// <summary>
        /// PublishOnly
        /// </summary>
        PublishOnly = 3
    }
}
