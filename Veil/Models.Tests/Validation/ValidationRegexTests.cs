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
            Assert.That(phoneNumber, Is.StringMatching(ValidationRegex.INPUT_PHONE));
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
            Assert.That(phoneNumber, Is.Not.StringMatching(ValidationRegex.INPUT_PHONE));
        }

        [TestCase("800-555-0199")]
        [TestCase("800-555-0199, ext. 5555")]
        [TestCase("800-555-0199, ext. 555")]
        [TestCase("800-555-0199, ext. 55")]
        [TestCase("800-555-0199, ext. 5")]
        public void StoredPhone_ValidInput_PassesRegex(string phoneNumber)
        {
            Assert.That(phoneNumber, Is.StringMatching(ValidationRegex.STORED_PHONE));
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
            Assert.That(phoneNumber, Is.Not.StringMatching(ValidationRegex.STORED_PHONE));
        }

        [TestCase("0000000000000")]
        [TestCase("0000012003400")]
        [TestCase("0569482748305")]
        [TestCase("0999999999999")]
        public void PhysicalGameProductNewSKU_ValidInput_PassesRegex(string newSKU)
        {
            Assert.That(newSKU, Is.StringMatching(ValidationRegex.PHYSICAL_GAME_PRODUCT_NEW_SKU));
        }

        [TestCase("1000000000000")]
        [TestCase("00000120034000")]
        [TestCase("9569482748305")]
        [TestCase("000000000000")]
        public void PhysicalGameProductNewSKU_InvalidInput_FailsRegex(string newSKU)
        {
            Assert.That(newSKU, Is.Not.StringMatching(ValidationRegex.PHYSICAL_GAME_PRODUCT_NEW_SKU));
        }

        [TestCase("1000000000000")]
        [TestCase("1000012003400")]
        [TestCase("1569482748305")]
        [TestCase("1999999999999")]
        public void PhysicalGameProductUsedSKU_ValidInput_PassesRegex(string usedSKU)
        {
            Assert.That(usedSKU, Is.StringMatching(ValidationRegex.PHYSICAL_GAME_PRODUCT_USED_SKU));
        }

        [TestCase("0000000000000")]
        [TestCase("10000120034000")]
        [TestCase("9569482748305")]
        [TestCase("100000000000")]
        public void PhysicalGameProductUsedSKU_InvalidInput_FailsRegex(string usedSKU)
        {
            Assert.That(usedSKU, Is.Not.StringMatching(ValidationRegex.PHYSICAL_GAME_PRODUCT_USED_SKU));
        }

        [TestCase("N2L6R2")]
        [TestCase("n2l6r2")]
        [TestCase("N2L-6R2")]
        [TestCase("n2l-6r2")]
        [TestCase("N2L 6R2")]
        [TestCase("n2l 6r2")]
        [TestCase("12345")]
        [TestCase("12345-6789")]
        [TestCase("12345 6789")]
        public void PostalZipCode_ValidInput_PassesRegex(string postalZipCode)
        {
            Assert.That(postalZipCode, Is.StringMatching(ValidationRegex.POSTAL_ZIP_CODE));
        }

        [TestCase("N2L  6R2")]
        [TestCase("n2l  6r2")]
        [TestCase("N2l -6r2")]
        [TestCase("N2L-6D2")]
        [TestCase("N2L-6F2")]
        [TestCase("N2L-6I2")]
        [TestCase("N2L-6O2")]
        [TestCase("N2L-6Q2")]
        [TestCase("N2L-6U2")]
        [TestCase("W2L-6R2")]
        [TestCase("Z2L-6R2")]
        [TestCase("123456789")]
        [TestCase("12345-678")]
        [TestCase("12345 678")]
        [TestCase("1234")]
        public void PostalZipCode_InvalidInput_FailsRegex(string postalZipCode)
        {
            Assert.That(postalZipCode, Is.Not.StringMatching(ValidationRegex.POSTAL_ZIP_CODE));
        }

        
        [TestCase("N2L 6R2")]
        [TestCase("12345")]
        [TestCase("12345-6789")]
        
        public void StoredPostalCode_ValidInput_PassesRegex(string postalZipCode)
        {
            Assert.That(postalZipCode, Is.StringMatching(ValidationRegex.STORED_POSTAL_CODE));
        }

        [TestCase("n2l 6r2")]
        [TestCase("N2L6R2")]
        [TestCase("n2l6r2")]
        [TestCase("N2L-6R2")]
        [TestCase("n2l-6r2")]
        [TestCase("N2L  6R2")]
        [TestCase("n2l  6r2")]
        [TestCase("N2l -6r2")]
        [TestCase("N2L-6D2")]
        [TestCase("N2L-6F2")]
        [TestCase("N2L-6I2")]
        [TestCase("N2L-6O2")]
        [TestCase("N2L-6Q2")]
        [TestCase("N2L-6U2")]
        [TestCase("W2L-6R2")]
        [TestCase("Z2L-6R2")]
        [TestCase("12345 6789")]
        [TestCase("123456789")]
        [TestCase("12345-678")]
        [TestCase("12345 678")]
        [TestCase("1234")]
        public void StoredPostalCode_InvalidInput_FailsRegex(string postalZipCode)
        {
            Assert.That(postalZipCode, Is.Not.StringMatching(ValidationRegex.STORED_POSTAL_CODE));
        }
    }
}
