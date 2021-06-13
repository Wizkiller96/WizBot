using System.Linq;
using WizBot.Core.Common;
using NUnit.Framework;

namespace WizBot.Tests
{
    public class KwumTests
    {
        [Test]
        public void TestDefaultHashCode()
        {
            var num = default(kwum);

            Assert.AreEqual(0, num.GetHashCode());
        }
        
        [Test]
        public void TestEqualGetHashCode()
        {
            var num1 = new kwum("234");
            var num2 = new kwum("234");

            Assert.AreEqual(num1.GetHashCode(), num2.GetHashCode());
        }
        
        [Test]
        public void TestNotEqualGetHashCode()
        {
            var num1 = new kwum("234");
            var num2 = new kwum("235");

            Assert.AreNotEqual(num1.GetHashCode(), num2.GetHashCode());
        }
        
        [Test]
        public void TestLongEqualGetHashCode()
        {
            var num1 = new kwum("hgbkhdbkhrbkrdhgbrdry664646464");
            var num2 = new kwum("hgbkhdbkhrbkrdhgbrdry664646464");

            Assert.AreEqual(num1.GetHashCode(), num2.GetHashCode());
        }
        
        [Test]
        public void TestLongEqual()
        {
            var num1 = new kwum("hgbkhdbkhrbkrdhgbrdry664646464");
            var num2 = new kwum("hgbkhdbkhrbkrdhgbrdry664646464");

            Assert.AreEqual(num1, num2);
        }
        
        [Test]
        public void TestLongNotEqual()
        {
            var num1 = new kwum("hgbkhdbkhrbkrdhgbrdry664646464");
            var num2 = new kwum("hgbkhdbkhrbkrdhgbrdry664647464");

            Assert.AreNotEqual(num1, num2);
        }
        
        [Test]
        public void TestParseValidValue()
        {
            var validValue = "234e";
            Assert.True(kwum.TryParse(validValue, out _));
        }

        [Test]
        public void TestParseInvalidValue()
        {
            var invalidValue = "1234";
            Assert.False(kwum.TryParse(invalidValue, out _));
        }
        
        [Test]
        public void TestCorrectParseValue()
        {
            var validValue = "qwerf4bmkjbvg67";
            kwum.TryParse(validValue, out var parsedValue);
            
            Assert.AreEqual(parsedValue, new kwum(validValue));
        }
        
        [Test]
        public void TestToString()
        {
            var validValue = "46g5yhhff254";
            kwum.TryParse(validValue, out var parsedValue);
            
            Assert.AreEqual(validValue, parsedValue.ToString());
        }

        [Test]
        public void TestConversionsToFromLong()
        {
            var num = new kwum(10);
            
            Assert.AreEqual(10, (long)num);
            Assert.AreEqual(num, (kwum)10);
        }

        [Test]
        public void TestConverstionsToString()
        {
            var num = new kwum(10);
            Assert.AreEqual("c", num.ToString());
            num = new kwum(123);
            Assert.AreEqual("5v", num.ToString());
            
            // leading zeros have no meaning
            Assert.AreEqual(new kwum("22225v"), num);
        }

        [Test]
        public void TestTooLong()
        {
            Assert.AreEqual((long)new kwum("zzzzzzzzzzz"), 5);
        }
    }
}