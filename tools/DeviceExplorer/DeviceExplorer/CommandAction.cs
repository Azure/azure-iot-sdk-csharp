using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceExplorer
{
    /// <summary>
    /// Actions for C2D messages
    /// </summary>
    internal enum CommandAction
    {
        None = 0,
        Complete = 1,
        Abandon = 2
    }
}
