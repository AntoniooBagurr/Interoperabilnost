using InterOp.Server.Data;
using InterOp.Server.Domain;
using InterOp.Server.Dto;
using InterOp.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace InterOp.Server.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsImportController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProductsImportController(AppDbContext db, IWebHostEnvironment env)
    { _db = db; _env = env; }

    [HttpPost("xsd")]
    [Consumes("text/plain")]
    public async Task<IActionResult> ImportWithXsd([FromBody] string xml, CancellationToken ct = default)
        => await ValidateDeserializeAndSave(xml, ct);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => (await _db.Products.FindAsync(id)) is { } p ? Ok(p) : NotFound();

    private async Task<IActionResult> ValidateDeserializeAndSave(string xml, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return BadRequest(new { message = "XML body je prazan." });

        var xsdPath = Path.Combine(_env.ContentRootPath, "XmlSchemas", "product.xsd");
        await using var xmlMs = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        await using var xsd = System.IO.File.OpenRead(xsdPath);

        var (ok, errors) = XmlValidationService.ValidateWithXsd(xmlMs, xsd);
        if (!ok) return UnprocessableEntity(new { message = "XSD validation failed", errors });

        xmlMs.Position = 0;
        var ser = new XmlSerializer(typeof(ProductXml));
        if (ser.Deserialize(xmlMs) is not ProductXml dto)
            return BadRequest(new { message = "XML deserialization failed" });

        if (await _db.Products.AnyAsync(p => p.ExtId == dto.Id, ct))
            return Conflict(new { message = "Product već postoji.", id = dto.Id });

        var entity = new Product
        {
            ExtId = dto.Id,
            Title = dto.Title,
            Currency = dto.Currency,
            Price = dto.Price,
            ShopName = dto.ShopName,
            Url = dto.Url,
            Pic = dto.Pic,
            Sales = dto.Sales,
            Reviews = dto.Reviews,
            CategoryId = dto.CategoryId,
            CategoryId2 = dto.CategoryId2,
            RawXml = xml
        };

        _db.Products.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Created($"/api/products/{entity.Id}", entity);
    }

    [HttpPost("rng")]
    [Consumes("text/plain", "application/xml")]
    public async Task<IActionResult> ImportWithRng([FromBody] string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return BadRequest(new { message = "Prazan XML." });

        await using var xmlStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        await using var rngStream =
            System.IO.File.OpenRead(Path.Combine(AppContext.BaseDirectory, "XmlSchemas", "product.rng"));

        var (ok, errors) = RelaxNgValidationService.Validate(xmlStream, rngStream);
        if (!ok) return BadRequest(new { errors });

        var xdoc = XDocument.Parse(xml);
        var root = xdoc.Root!;
        var product = new InterOp.Server.Domain.Product
        {
            ExtId = root.Element("Id")?.Value ?? "",
            Title = root.Element("Title")?.Value ?? "",
            Currency = root.Element("Currency")?.Value ?? "",
            Price = decimal.Parse(root.Element("Price")!.Value, CultureInfo.InvariantCulture),
            ShopName = root.Element("ShopName")?.Value ?? "",
            Url = root.Element("Url")?.Value ?? "",
            Pic = root.Element("Pic")?.Value ?? "",
            Sales = root.Element("Sales")?.Value ?? "",
            Reviews = root.Element("Reviews")?.Value ?? "",
            CategoryId = root.Element("CategoryId")?.Value ?? "",
            CategoryId2 = root.Element("CategoryId2")?.Value ?? "",
            RawXml = xml,
            CreatedUtc = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return Created($"/api/products/{product.Id}", product);
    }

}
