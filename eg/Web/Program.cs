using System.Text;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", ([FromQuery]string? name) =>
    Results.Text(HelloWorldTemplate.Render(!string.IsNullOrEmpty(name) ? name : "world"),
                 "text/html"));

app.Run();

abstract class Template
{
    readonly StringBuilder _sb;

    protected Template(StringBuilder? sb = null) =>
        _sb = sb ?? new StringBuilder();

    protected void WriteText(string value) => _sb.Append(value);
    protected void WriteValue(object value) => _sb.Append(value);

    public override string ToString() => _sb.ToString();
}

partial class HelloWorldTemplate : Template
{
    HelloWorldTemplate(string name, StringBuilder? sb = null) : base(sb)
    {
        Name = name;
    }

    public string Name { get; }

    public static string Render(string name)
    {
        var template = new HelloWorldTemplate(name);
        template.RenderCore();
        return template.ToString();
    }
}
