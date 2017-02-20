using System;
using System.Runtime.Serialization;

namespace IndoorRouting
{
    [Serializable]
    class Exception : System.Exception
    {
        public Exception()
        {
        }

        public Exception(string message) : base(message)
        {
        }

        public Exception(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        protected Exception(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}