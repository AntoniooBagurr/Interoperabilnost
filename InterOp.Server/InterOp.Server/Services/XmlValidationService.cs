using System.Xml;
using System.Xml.Schema;

namespace InterOp.Server.Services;

public static class XmlValidationService
{
    public static (bool Ok, List<string> Errors) ValidateWithXsd(Stream xmlStream, Stream xsdStream)
    {
        var errors = new List<string>();
        var schemas = new XmlSchemaSet();
        schemas.Add(targetNamespace: "", XmlReader.Create(xsdStream));

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = schemas,
            DtdProcessing = DtdProcessing.Prohibit
        };
        settings.ValidationEventHandler += (_, e) =>
        {
            var sev = e.Severity == XmlSeverityType.Error ? "Error" : "Warning";
            errors.Add($"{sev}: {e.Message}");
        };

        using var reader = XmlReader.Create(xmlStream, settings);
        try
        {
            while (reader.Read()) { /* čitanje trigira validaciju */ }
            return (!errors.Any(), errors);
        }
        catch (XmlException ex)
        {
            errors.Add($"XmlException: {ex.Message}");
            return (false, errors);
        }
    }
}
