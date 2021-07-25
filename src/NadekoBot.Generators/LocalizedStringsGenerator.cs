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
    internal class FieldData
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }
    
    [Generator]
    public class LocalizedStringsGenerator : ISourceGenerator
    {
        private const string LocStrSource = @"namespace NadekoBot
{
    public readonly ref struct LocStr0
    {
        public readonly string Key;
        
        public LocStr0(string key)
        {
            Key = key;
        }
        
        public static implicit operator LocStr0(string data)
            => new LocStr0(data);    
    }
    
    public readonly ref struct LocStr1
    {
        public readonly string Key;
        
        public LocStr1(string key)
        {
            Key = key;
        }
        
        public static implicit operator LocStr1(string data)
            => new LocStr1(data);    
    }
    
    public readonly ref struct LocStr2
        {
            public readonly string Key;
            
            public LocStr2(string key)
            {
                Key = key;
            }
            
            public static implicit operator LocStr2(string data)
                => new LocStr2(data);    
        }
        
        public readonly ref struct LocStr3
    {
        public readonly string Key;
        
        public LocStr3(string key)
        {
            Key = key;
        }
        
        public static implicit operator LocStr3(string data)
            => new LocStr3(data);    
    }
    
    public readonly ref struct LocStr4
    {
        public readonly string Key;
        
        public LocStr4(string key)
        {
            Key = key;
        }
        
        public static implicit operator LocStr4(string data)
            => new LocStr4(data);    
    }
    
    public readonly ref struct LocStr5
    {
        public readonly string Key;
        
        public LocStr5(string key)
        {
            Key = key;
        }
        
        public static implicit operator LocStr5(string data)
            => new LocStr5(data);    
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
                    sw.WriteLine($"public static {field.Type} {field.Name} => \"{field.Name}\";");
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

        private List<FieldData> GetFields(string dataText)
        {
            if (string.IsNullOrWhiteSpace(dataText))
                throw new ArgumentNullException(nameof(dataText));
            
            var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataText);

            var list = new List<FieldData>();
            foreach (var entry in data)
            {
                list.Add(new FieldData()
                {
                    Type = GetFieldType(entry.Value),
                    Name = entry.Key,
                });
            }

            return list;
        }

        private string GetFieldType(string value)
        {
            var matches = Regex.Matches(value, @"{(?<num>\d)}");
            int max = -1;
            foreach (Match match in matches)
            {
                max = Math.Max(max, int.Parse(match.Groups["num"].Value));
            }

            max += 1;
            if (max == 0)
                return "LocStr0";
            if (max == 1)
                return "LocStr1";
            if (max == 2)
                return "LocStr2";
            if (max == 3)
                return "LocStr3";
            if (max == 4)
                return "LocStr4";
            if (max == 5)
                return "LocStr5";

            return "!Error";
        }
    }
}