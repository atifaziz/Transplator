# Transplator

This project is currently an experiment into the idea of generating C# source
code from a text template. The code generation is integrated into the C#
compiler using the [source generator] feature made available starting with
the version supporting C# 9.

The input to Transplator is a simple text template file, where literal text is
interspersed with C# source code. The output is C# code that, when executed as
part of the compiled product, will produce text with literal parts copied over
as they are and the C# code in the template contributing dynamic text.

Using the [source generator] feature has the benefit that the template is
turned into strong- and statically-typed C# source code. Any type errors will
be caught at compile-time. At run-time, the template output will be produced
at native speed, without any interpretation.

The text templates can be used, for example, to generate e-mail messages with
certain parts replaced dynamically at run-time.

## Format

Anything between `{%` and `%}` is considered C# code and everthing else is
treated as literal text. For example:

    The current date and time is {% DateTime.Now %}.

Transplator has some smarts to distinguish between control flow blocks or
statement and expressions that contribute dynamic text, so syntactically,
you only need to remember to use `{%` and `%}` as delimiters:

    {%
        for (var i = 0; i < 10; i++) {
    %}
            i = {% i + 1 %}
    {%
        }
    %}

Like the [Liquid] templating language, you can use `-` to trim whitespace
from the left or right side:

    {%- "left whitespace is trimmed" %}
    {% "right whitespace is trimmed" -%}
    {%- "left and right whitespace is trimmed" -%}

Like the [Scriban] templating language, you can use `~` to for [non-greedy
whitespace trimming][scrws]:

> - Using a `{{~` will remove any whitespace before but will stop on the
>   first newline without including it
> - Using a `~}}` will remove any whitespace after including the first
>   newline but will stop after


  [source generator]: https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/
  [Liquid]: https://shopify.github.io/liquid/
  [Scriban]: https://github.com/scriban/scriban
  [scrws]: https://github.com/scriban/scriban/blob/master/doc/language.md#14-whitespace-control
