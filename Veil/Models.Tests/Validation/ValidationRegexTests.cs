using System.Text.RegularExpressions;
using NUnit.Framework;
using Veil.DataModels.Validation;

namespace Veil.DataModels.Tests.Validation
{
    [TestFixture]
    public class ValidationRegexTests
    {
        [TestCase("800-555-0199")]
        [TestCase("800-555-0199, ext. 5555")]
        [TestCase("800-555-0199, ext. 555")]
        [TestCase("800-555-0199, ext. 55")]
        [TestCase("800-555-0199, ext. 5")]
        [TestCase("(800)555-0199")]
        [TestCase("(800)555-0199, ext. 5555")]
        [TestCase("(800)555-0199, ext. 555")]
        [TestCase("(800)555-0199, ext. 55")]
        [TestCase("(800)555-0199, ext. 5")]
        [TestCase("800-555-0199, EXT. 5555")]
        [TestCase("800-555-0199, EXT. 555")]
        [TestCase("800-555-0199, EXT. 55")]
        [TestCase("800-555-0199, EXT. 5")]
        [TestCase("(800)555-0199, EXT. 5555")]
        [TestCase("(800)555-0199, EXT. 555")]
        [TestCase("(800)555-0199, EXT. 55")]
        [TestCase("(800)555-0199, EXT. 5")]
        public void InputPhone_ValidInput_PassesRegex(string phoneNumber)
        {
            Assert.That(Regex.IsMatch(phoneNumber, ValidationRegex.INPUT_PHONE), Is.True);
        }

        [TestCase("(800) 555-0199")]
        [TestCase("(800)-555-0199")]
        [TestCase("(800)-555 0199")]
        [TestCase("(800) 555 0199")]
        [TestCase("800 555 0199")]
        [TestCase("800 555 0199, ext. ")]
        [TestCase("800 555 0199, EXT. ")]
        [TestCase("800 555 0199, ext. 55555")]
        [TestCase("800 555 0199, EXT. 55555")]
        [TestCase("800-555-0199, x5555")]
        [TestCase("800-555-0199, X5555")]
        [TestCase("800-555-0199, ext5555")]
        [TestCase("800-555-0199, EXT5555")]
        [TestCase("(800)555-0199, x. 5555")]
        [TestCase("(800)555-0199 EXT. 5")]
        [TestCase("(800)555-0199 ext. 5555")]
        [TestCase("800-555-0199 ext. 555")]
        public void InputPhone_InvalidInput_FailsRegex(string phoneNumber)
        {
            Assert.That(Regex.IsMatch(phoneNumber, ValidationRegex.INPUT_PHONE), Is.False);
        }

        [TestCase("800-555-0199")]
        [TestCase("800-555-0199, ext. 5555")]
        [TestCase("800-555-0199, ext. 555")]
        [TestCase("800-555-0199, ext. 55")]
        [TestCase("800-555-0199, ext. 5")]
        public void StoredPhone_ValidInput_PassesRegex(string phoneNumber)
        {
            Assert.That(Regex.IsMatch(phoneNumber, ValidationRegex.STORED_PHONE), Is.True);
        }

        [TestCase("(800) 555-0199")]
        [TestCase("(800)-555-0199")]
        [TestCase("(800)-555 0199")]
        [TestCase("(800) 555 0199")]
        [TestCase("800 555 0199")]
        [TestCase("800 555 0199, ext. ")]
        [TestCase("800 555 0199, EXT. ")]
        [TestCase("800 555 0199, ext. 55555")]
        [TestCase("800 555 0199, EXT. 55555")]
        [TestCase("800-555-0199, x5555")]
        [TestCase("800-555-0199, X5555")]
        [TestCase("800-555-0199, ext5555")]
        [TestCase("800-555-0199, EXT5555")]
        [TestCase("(800)555-0199, x. 5555")]
        [TestCase("(800)555-0199, EXT. 5555")]
        [TestCase("(800)555-0199, EXT. 555")]
        [TestCase("(800)555-0199, EXT. 55")]
        [TestCase("(800)555-0199, EXT. 5")]
        [TestCase("(800)555-0199")]
        [TestCase("(800)555-0199, ext. 5555")]
        [TestCase("(800)555-0199, ext. 555")]
        [TestCase("(800)555-0199, ext. 55")]
        [TestCase("(800)555-0199, ext. 5")]
        [TestCase("800-555-0199, EXT. 5555")]
        [TestCase("800-555-0199, EXT. 555")]
        [TestCase("800-555-0199, EXT. 55")]
        [TestCase("800-555-0199, EXT. 5")]
        [TestCase("800-555-0199 ext. 5")]
        public void StoredPhone_InvalidInput_FailsRegex(string phoneNumber)
        {
            Assert.That(Regex.IsMatch(phoneNumber, ValidationRegex.STORED_PHONE), Is.False);
        }

        [TestCase("0000000000000")]
        [TestCase("0000012003400")]
        [TestCase("0569482748305")]
        [TestCase("0999999999999")]
        public void PhysicalGameProductNewSKU_ValidInput_PassesRegex(string newSKU)
        {
            Assert.That(Regex.IsMatch(newSKU, ValidationRegex.PHYSICAL_GAME_PRODUCT_NEW_SKU), Is.True);
        }

        [TestCase("1000000000000")]
        [TestCase("00000120034000")]
        [TestCase("9569482748305")]
        [TestCase("000000000000")]
        public void PhysicalGameProductNewSKU_InvalidInput_FailsRegex(string newSKU)
        {
            Assert.That(Regex.IsMatch(newSKU, ValidationRegex.PHYSICAL_GAME_PRODUCT_NEW_SKU), Is.False);
        }

        [TestCase("1000000000000")]
        [TestCase("1000012003400")]
        [TestCase("1569482748305")]
        [TestCase("1999999999999")]
        public void PhysicalGameProductUsedSKU_ValidInput_PassesRegex(string usedSKU)
        {
            Assert.That(Regex.IsMatch(usedSKU, ValidationRegex.PHYSICAL_GAME_PRODUCT_USED_SKU), Is.True);
        }

        [TestCase("0000000000000")]
        [TestCase("10000120034000")]
        [TestCase("9569482748305")]
        [TestCase("100000000000")]
        public void PhysicalGameProductUsedSKU_InvalidInput_FailsRegex(string usedSKU)
        {
            Assert.That(Regex.IsMatch(usedSKU, ValidationRegex.PHYSICAL_GAME_PRODUCT_USED_SKU), Is.False);
        }
    }
}
