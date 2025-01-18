// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.IO;
// using System.Linq;
// using Wiz.Common;
// using WizBot.Modules.Games;
// using NUnit.Framework;
//
// namespace WizBot.Tests;
//
// public class FishTests
// {
//     [Test]
//     public void TestWeather()
//     {
//         var fs = new FishService(null, null);
//
//         var rng = new Random();
//
//         // output = @"ro+dD:bN0uVqV3ZOAv6r""EFeA'A]u]uSyz2Qd'r#0Vf:5zOX\VgSsF8LgRCL/uOW";
//         while (true)
//         {
//             var output = "";
//             for (var i = 0; i < 64; i++)
//             {
//                 var c = (char)rng.Next(33, 123);
//                 output += c;
//             }
//
//             output = "";
//             var weathers = new List<FishingWeather>();
//             for (var i = 0; i < 1_000_000; i++)
//             {
//                 var w = fs.GetWeather(DateTime.UtcNow.AddHours(6 * i), output);
//                 weathers.Add(w);
//             }
//
//             var vals = weathers.GroupBy(x => x)
//                 .ToDictionary(x => x.Key, x => x.Count());
//
//             var str = weathers.Select(x => (int)x).Join("");
//             var maxLength = MaxLength(str);
//
//             if (maxLength < 12)
//             {
//                 foreach (var v in vals)
//                 {
//                     Console.WriteLine($"{v.Key}: {v.Value}");
//                 }
//
//                 Console.WriteLine(output);
//                 Console.WriteLine(maxLength);
//
//                 File.WriteAllText("data.txt", weathers.Select(x => (int)x).Join(""));
//
//                 break;
//             }
//         }
//     }
//
//     // string with same characters
//     static int MaxLength(String s)
//     {
//         int ans = 1, temp = 1;
//
//         // Traverse the string
//         for (int i = 1; i < s.Length; i++)
//         {
//             // If character is same as
//             // previous increment temp value
//             if (s[i] == s[i - 1])
//             {
//                 ++temp;
//             }
//             else
//             {
//                 ans = Math.Max(ans, temp);
//                 temp = 1;
//             }
//         }
//
//         ans = Math.Max(ans, temp);
//
//         // Return the required answer
//         return ans;
//     }
// }