using System.Text;
using System.Xml.Serialization;
using InterOp.Server.Data;
using InterOp.Server.Domain;
using InterOp.Server.Dto;
using InterOp.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InterOp.Server.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsImportController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProductsImportController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db; _env = env;
    }
    [HttpPost("import/xsd")]
    public async Task<IActionResult> ImportWithXsd()
    {
        if (Request.ContentType?.Contains("xml") != true)
            return BadRequest("Content-Type mora biti application/xml.");

        using var mem = new MemoryStream();
        await Request.Body.CopyToAsync(mem);
        mem.Position = 0;

        var xsdPath = Path.Combine(_env.ContentRootPath, "XmlSchemas", "product.xsd");
        await using var xsd = System.IO.File.OpenRead(xsdPath);

        var (ok, errors) = XmlValidationService.ValidateWithXsd(mem, xsd);
        if (!ok)
            return UnprocessableEntity(new { message = "XSD validacija nije prošla.", errors });

        mem.Position = 0;
        var ser = new XmlSerializer(typeof(ProductXml));
        ProductXml? dto;
        try
        {
            dto = (ProductXml?)ser.Deserialize(mem);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Ne mogu deserijalizirati XML.", error = ex.Message });
        }

        if (dto == null)
            return BadRequest("Prazan ili neispravan XML.");

        var entity = new Product
        {
            ExtId = dto.Id,
            Title = dto.Title,
            Currency = dto.Currency,
            Price = dto.Price,
            ShopName = dto.ShopName,
            Url = dto.Url
        };

        var exists = await _db.Products.AnyAsync(p => p.ExtId == entity.ExtId);
        if (exists)
            return Conflict(new { message = $"Product s ExtId={entity.ExtId} već postoji." });

        _db.Products.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.Products.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }
}
