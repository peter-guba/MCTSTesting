using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Benchmark.Exceptions
{
    internal class InvalidXmlDataException : Exception
    {
        public InvalidXmlDataException(string message)
            : base (message)
        {
        }
    }
}
