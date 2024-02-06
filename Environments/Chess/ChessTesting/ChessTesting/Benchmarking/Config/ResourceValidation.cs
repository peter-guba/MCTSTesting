using System.IO;
using Benchmarking.Exceptions;

namespace Benchmarking.Config
{
    /// <summary>
    /// Class with utility methods for resource validation.
    /// </summary>
    internal static class ResourceValidation
    {
        /// <summary>
        /// Valudats whether resource file with given name exists.
        /// </summary>
        /// <param name="file"></param>
        public static void CheckResource(string file)
        {
            if (!File.Exists(file))
            {
                throw new ResourceMissingException(file);
            }
        }
    }
}
