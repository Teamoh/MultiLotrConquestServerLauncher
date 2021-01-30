using System;
using System.Runtime.Serialization;

namespace MultiLotrConquestServerLauncher {
    [Serializable]
    internal class MultiServerException : Exception {
        public MultiServerException() {
        }

        public MultiServerException(string message) : base(message) {
        }

        public MultiServerException(string message, Exception innerException) : base(message, innerException) {
        }

        protected MultiServerException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}