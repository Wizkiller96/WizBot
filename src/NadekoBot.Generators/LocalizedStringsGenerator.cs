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
    public readonly ref struct LocStr
    {
        public readonly string Key;
        
        public LocStr(string key)
        {
            Key = key;
        }
        
        public static implicit operator LocStr(string data)
            => new LocStr(data);    
    }
    
    public readonly ref struct LocStr<T1>
    {
        public readonly string Key;
        
        public LocStr(string key)
        {
            Key = key;
        }
        
        public static implicit operator LocStr<T1>(string data)
            => new LocStr<T1>(data);    
    }
    
    public readonly ref struct LocStr<T1, T2>
    {
        public readonly string Key;
        
        public LocStr(string key)
        {
            Key = key;
        }
        
        public static implicit operator LocStr<T1, T2>(string data)
            => new LocStr<T1, T2>(data);    
    }
    
    public readonly ref struct LocStr<T1, T2, T3>
    {
        public readonly string Key;
        
        public LocStr(string key)
        {
            Key = key;
        }
        
        public static implicit operator LocStr<T1, T2, T3>(string data)
            => new LocStr<T1, T2, T3>(data);    
    }
    
    public readonly ref struct LocStr<T1, T2, T3, T4>
    {
        public readonly string Key;
        
        public LocStr(string key)
        {
            Key = key;
        }
        
        public static implicit operator LocStr<T1, T2, T3, T4>(string data)
            => new LocStr<T1, T2, T3, T4>(data);    
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
                return "LocStr";
            if (max == 1)
                return "LocStr<object>";
            if (max == 2)
                return "LocStr<object, object>";
            if (max == 3)
                return "LocStr<object, object, object>";
            if (max == 4)
                return "LocStr<object, object, object, object>";

            return "!Error";
        }
    }
}