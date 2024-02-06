using System;

namespace Benchmarking.Exceptions
{
    internal class ResourceMissingException : Exception
    {
        public string Resource { get; }

        public ResourceMissingException(string resource)
            : base($"Resource missing: {resource}")
        {
            Resource = resource;
        }
    }
}
