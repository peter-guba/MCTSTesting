using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Benchmark.Exceptions
{
    internal class InvalidResourceReferenceException : Exception
    {
        public string Resource { get; }
        public string Reference { get; }

        public InvalidResourceReferenceException(string resource, string refernce)
            : base($"Resource {resource} referenced from {refernce} not found.")
        {
            Resource = resource;
            Reference = refernce;
        }
    }
}
