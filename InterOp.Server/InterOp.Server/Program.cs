using InterOp.Server.Data;
using InterOp.Server.Formatters;
using InterOp.Server.Services;
using InterOp.Server.Services.Soap;
using InterOp.Server.Dto;                
using Microsoft.EntityFrameworkCore;
using SoapCore;
using Microsoft.AspNetCore.Routing;

using SoapSettings = InterOp.Server.Dto.SoapOptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("sql")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<TaobaoService>();
builder.Services.AddHttpClient<TaobaoBasicService>();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o => { o.TimestampFormat = "HH:mm:ss "; o.SingleLine = true; });

// SOAP
builder.Services.AddSoapCore();
builder.Services.Configure<SoapSettings>(builder.Configuration.GetSection("Soap"));
builder.Services.AddSingleton<IProductSoapService, ProductSoapService>();

builder.Services.AddControllers(options =>
{
    options.InputFormatters.Insert(0, new PlainTextInputFormatter());
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

IEndpointRouteBuilder endpoints = app;
endpoints.UseSoapEndpoint<IProductSoapService>(
    "/soap/products.svc",
    new SoapEncoderOptions(),
    SoapSerializer.DataContractSerializer);

app.MapControllers();
app.Run();
