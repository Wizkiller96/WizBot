using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;

namespace NadekoBot.Generators
{
    internal class TranslationPair
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    [Generator]
    public class LocalizedStringsGenerator : ISourceGenerator
    {
        private const string LocStrSource = @"namespace NadekoBot
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
                sw.WriteLine("namespace NadekoBot");
                sw.WriteLine("{");
                sw.Indent++;

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
                sw.Indent--;
                sw.WriteLine("}");


                sw.Flush();
                context.AddSource("strs.cs", stringWriter.ToString());
            }

            context.AddSource("LocStr.cs", LocStrSource);
        }

        private List<TranslationPair> GetFields(string dataText)
        {
            if (string.IsNullOrWhiteSpace(dataText))
                throw new ArgumentNullException(nameof(dataText));

            var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataText);

            var list = new List<TranslationPair>();
            foreach (var entry in data)
            {
                list.Add(new TranslationPair()
                {
                    Name = entry.Key,
                    Value = entry.Value
                });
            }

            return list;
        }
    }
}