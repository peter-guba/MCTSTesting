using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarking.Exceptions
{
    internal class InvalidResourceReferenceException : Exception
    {
        public string Resource { get; }
        public string Reference { get; }

        public InvalidResourceReferenceException(string resource, string reference)
            : base($"Resource {resource} referenced from {reference} not found.")
        {
            Resource = resource;
            Reference = reference;
        }
    }
}
