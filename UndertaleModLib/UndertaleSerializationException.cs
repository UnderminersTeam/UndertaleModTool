using System;
using System.Runtime.Serialization;

namespace UndertaleModLib
{
    [Serializable]
    internal class UndertaleSerializationException : Exception
    {
        public UndertaleSerializationException()
        {
        }

        public UndertaleSerializationException(string message) : base(message)
        {
        }

        public UndertaleSerializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}