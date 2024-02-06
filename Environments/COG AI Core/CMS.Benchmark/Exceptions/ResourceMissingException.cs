using System;

namespace CMS.Benchmark.Exceptions
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
