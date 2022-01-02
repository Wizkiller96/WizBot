#nullable enable
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace NadekoBot.Generators
{
    internal readonly struct TranslationPair
    {
        public string Name { get; }
        public string Value { get; }

        public TranslationPair(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    [Generator]
    public class LocalizedStringsGenerator : ISourceGenerator
    {
        private const string LOC_STR_SOURCE = @"namespace NadekoBot
{
    public readonly struct LocStr
    {
        public readonly string Key;
        public readonly object[] Params;
        
        public LocStr(string key, params object[] data)
        {
            Key = key;
            Params = data;
        }
    }
}";

        public void Initialize(GeneratorInitializationContext context)
        {

        }

        public void Execute(GeneratorExecutionContext context)
        {
            var file = context.AdditionalFiles.First(x => x.Path.EndsWith("responses.en-US.json"));

            var fields = GetFields(file.GetText()?.ToString());

            using (var stringWriter = new StringWriter())
            using (var sw = new IndentedTextWriter(stringWriter))
            {
                sw.WriteLine("namespace NadekoBot;");
                sw.WriteLine();

                sw.WriteLine("public static class strs");
                sw.WriteLine("{");
                sw.Indent++;

                foreach (var field in fields)
                {
                    var matches = Regex.Matches(field.Value, @"{(?<num>\d)[}:]");
                    var max = 0;
                    foreach (Match match in matches)
                    {
                        max = Math.Max(max, int.Parse(match.Groups["num"].Value) + 1);
                    }

                    List<string> typedParamStrings = new List<string>();
                    var paramStrings = string.Empty;
                    for (var i = 0; i < max; i++)
                    {
                        typedParamStrings.Add($"object p{i}");
                        paramStrings += $", p{i}";
                    }


                    var sig = string.Empty;
                    if(max > 0)
                        sig = $"({string.Join(", ", typedParamStrings)})";
                    
                    sw.WriteLine($"public static LocStr {field.Name}{sig} => new LocStr(\"{field.Name}\"{paramStrings});");
                }

                sw.Indent--;
                sw.WriteLine("}");


                sw.Flush();
                context.AddSource("strs.g.cs", stringWriter.ToString());
            }

            context.AddSource("LocStr.g.cs", LOC_STR_SOURCE);
        }

        private List<TranslationPair> GetFields(string? dataText)
        {
            if (string.IsNullOrWhiteSpace(dataText))
                return new();

            Dictionary<string, string> data;
            try
            {
                var output = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataText!);
                if (output is null)
                    return new();

                data = output;
            }
            catch
            {
                Debug.WriteLine("Failed parsing responses file.");
                return new();
            }

            var list = new List<TranslationPair>();
            foreach (var entry in data)
            {
                list.Add(new(
                    entry.Key,
                    entry.Value
                ));
            }

            return list;
        }
    }
}