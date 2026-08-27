using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionManagementSystem.Core.Enums {
    /// <summary>
    /// A client only ever receives versions published to its configured channel.
    /// </summary>
    public enum UpdateChannel {
        Stable = 0,
        Beta = 1,
        Development = 2
    }
}
