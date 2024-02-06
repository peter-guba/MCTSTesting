using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace CMS.Benchmark.Config
{
    /// <summary>
    /// Utility class used for validating XML files against XSD.
    /// </summary>
    internal static class XmlValidation
    {
        private static bool _isValid { get; set; } = true;

        private static readonly ValidationEventHandler ValidationErrorHandler = SchemaErrorHandler;

        private static string _docFile;

        private static XmlSchemaSet _typeSchema;

        public static XmlSchemaSet TypeSchema
        {
            get
            {
                Initialize();
                return _typeSchema;
            }
        }

        private static bool _isInitialized;

        public static void Initialize()
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                string schemaFile = "../../../../../../Resources/SchemaTypes/Types.xsd";
                ResourceValidation.CheckResource(schemaFile);
                _typeSchema = new XmlSchemaSet();
                _typeSchema.Add("", XmlReader.Create(new StreamReader(schemaFile)));
            }
        }

        private static void SchemaErrorHandler(object sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Warning)
                return;

            if (_isValid)
            {
                _isValid = false;
                Console.WriteLine($"Xml document {_docFile} is not valid against schema: ");
            }
            Console.WriteLine($"  {e.Severity} on line {e.Exception.LineNumber}: {e.Message}");
        }

        /// <summary>
        /// Validates XML against given XSD.
        /// </summary>
        /// <param name="doc">XML document to validate.</param>
        /// <param name="schema">XSD against which to validate.</param>
        /// <param name="docFile">Name of the file containing <paramref name="doc"/>. (Used to format error messages.)</param>
        public static void ValidateDocument(XDocument doc, XmlSchemaSet schema, string docFile)
        {
            Initialize();
            _docFile = docFile;
            doc.Validate(schema, ValidationErrorHandler, true);
            if (!_isValid)
            {
                _isValid = true;
                throw new XmlSchemaValidationException();
            }
        }
    }
}
