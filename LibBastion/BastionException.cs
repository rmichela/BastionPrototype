using System;

namespace LibBastion
{
    public class BastionException : Exception
    {
        public BastionException(string message) : base(message) { }
        public BastionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
