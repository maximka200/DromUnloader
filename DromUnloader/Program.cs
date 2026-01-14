using DromUnloader.XmlParser;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddSingleton<XmlParser>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/feed.xml", async ([FromServices] XmlParser parser, CancellationToken ct) =>
{
    var xml = await parser.BuildAvitoFeedStringAsync(ct);
    return Results.Text(xml, "application/xml; charset=utf-8");
});

app.Run();
