using System.Xml;
using System.Xml.Linq;
using System.Globalization;

namespace InterOp.Server.Services;

public static class RelaxNgValidationService
{
    private sealed record Field(string Name, string Type, bool Optional);

    public static (bool Ok, List<string> Errors) Validate(Stream xmlStream, Stream rngStream)
    {
        var errors = new List<string>();

        List<Field> spec;
        try
        {
            spec = ParseRng(rngStream);
        }
        catch (Exception ex)
        {
            errors.Add($"RNG load/parse error: {ex.Message}");
            return (false, errors);
        }

        XDocument xdoc;
        try
        {
            xmlStream.Position = 0;
            xdoc = XDocument.Load(xmlStream, LoadOptions.SetLineInfo);
        }
        catch (Exception ex)
        {
            errors.Add($"XML parse error: {ex.Message}");
            return (false, errors);
        }

        var root = xdoc.Root;
        if (root == null || root.Name.LocalName != "Product")
        {
            errors.Add("Root element mora biti <Product>.");
            return (false, errors);
        }

        foreach (var f in spec)
        {
            var el = root.Element(f.Name);
            if (el == null)
            {
                if (!f.Optional) errors.Add($"Nedostaje element <{f.Name}>.");
                continue;
            }

            var val = (el.Value ?? "").Trim();

            if (f.Type.Equals("decimal", StringComparison.OrdinalIgnoreCase))
            {
                if (!decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    errors.Add($"Element <{f.Name}> mora biti decimal.");
            }
            else
            {
                if (!f.Optional && string.IsNullOrWhiteSpace(val))
                    errors.Add($"Element <{f.Name}> ne smije biti prazan.");
            }
        }

        return (errors.Count == 0, errors);
    }

    private static List<Field> ParseRng(Stream rngStream)
    {
        var doc = XDocument.Load(rngStream);
        var rng = doc.Root ?? throw new InvalidOperationException("Prazan RNG dokument.");

        var productRule = rng
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "element" &&
                                 (string?)e.Attribute("name") == "Product")
            ?? throw new InvalidOperationException("RNG ne sadrži element name=\"Product\".");

        var fields = new List<Field>();
        ParseChildren(productRule, optional: false, fields);
        return fields;
    }

    private static void ParseChildren(XElement node, bool optional, List<Field> fields)
    {
        foreach (var ch in node.Elements())
        {
            var ln = ch.Name.LocalName;

            if (ln is "interleave" or "group")
            {
                ParseChildren(ch, optional, fields);
            }
            else if (ln == "optional")
            {
                ParseChildren(ch, optional: true, fields);
            }
            else if (ln == "element")
            {
                var name = (string?)ch.Attribute("name");
                if (string.IsNullOrWhiteSpace(name)) continue;

                string type = "string";
                var data = ch.Descendants().FirstOrDefault(e => e.Name.LocalName == "data");
                var text = ch.Descendants().FirstOrDefault(e => e.Name.LocalName == "text");

                if (data != null)
                    type = (string?)data.Attribute("type") ?? "string";
                else if (text != null)
                    type = "string";

                fields.Add(new Field(name!, type, optional));
            }
        }
    }
}
