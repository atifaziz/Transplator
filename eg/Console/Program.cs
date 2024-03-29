using System;
using System.Text;

Console.Write(HelloWorldTemplate.Render());
Console.Out.Flush();

Console.Write(ModelTemplate.Render(DateTime.Now));
Console.Out.Flush();

Console.Write(SimpleLoopTemplate.Render());
Console.Out.Flush();

Console.Write(ParametersTemplate.Render(new[]
{
    "First", "Second", "Third", "Fourth", "Fifth",
    "Sixth", "Seventh", "Eighth", "Ninth", "Tenth",
}));
Console.Out.Flush();

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
    HelloWorldTemplate(StringBuilder? sb = null) : base(sb) {}

    public static string Render()
    {
        var template = new HelloWorldTemplate();
        template.RenderCore();
        return template.ToString();
    }
}

partial class ModelTemplate : Template
{
    ModelTemplate(DateTime time, StringBuilder? sb = null) : base(sb) =>
        Time = time;

    DateTime Time { get; }

    public static string Render(DateTime time)
    {
        var template = new ModelTemplate(time);
        template.RenderCore();
        return template.ToString();
    }
}

partial class SimpleLoopTemplate : Template
{
    SimpleLoopTemplate(StringBuilder? sb = null) : base(sb) {}

    public static string Render()
    {
        var template = new SimpleLoopTemplate();
        template.RenderCore();
        return template.ToString();
    }
}
