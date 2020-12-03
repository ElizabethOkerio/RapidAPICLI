using Microsoft.OData.Edm.Csdl;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace RapidApi.Cli.Common
{
    public class SchemaValidator
    {
        public static void ValidateSchema(string path)
        {
            using var xmlReader = XmlReader.Create(path);

            try
            {
                CsdlReader.Parse(xmlReader);
            }
            catch (Exception e)
            {
                throw new RapidApiException($"Invalid schema file: {e.Message}");
            }
        }
    }
}
