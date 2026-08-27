using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionManagementSystem.Client.Exceptions {
    public sealed class UpdateCheckException : Exception {
        public UpdateCheckException(string message) : base(message) {

        }

        public UpdateCheckException(string message, Exception innerException) : base(message, innerException) {
        
        }
    }

    public sealed class PackageDownloadException : Exception {
        public PackageDownloadException(string message) : base(message) {
        
        }

        public PackageDownloadException(string message, Exception innerException) : base(message, innerException) {
        
        }
    }
}
