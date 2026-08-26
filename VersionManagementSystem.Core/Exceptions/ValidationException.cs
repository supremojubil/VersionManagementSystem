using System;

namespace VersionManagementSystem.Core.Exceptions {
    public sealed class ValidationException : Exception {
        public ValidationException(string message) : base(message) { }
    }
}
