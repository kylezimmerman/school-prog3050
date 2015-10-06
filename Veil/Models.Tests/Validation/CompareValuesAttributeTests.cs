/* CompareValuesAttributeTests.cs
 * Purpose: Unit tests for the CompareValuesAttribute
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NUnit.Framework;
using Veil.DataModels.Validation;

namespace Veil.DataModels.Tests.Validation
{
    public class ComparisonEntity
    {
        [DisplayName("MinDisplayName")]
        public int Minimum { get; set; }

        [Display(Name = "MaxDisplay")]
        public int Maximum { get; set; }

        public string StringType { get; set; }

        public string DefaultToNull { get; set; }

        public NonComparable Uncomparable { get; set; }

        public NonComparable OtherUncomparable { get; set; }
    }

    public struct NonComparable { }

    [TestFixture]
    public class CompareValuesAttributeTests
    {
        private ComparisonEntity CreateComparisonEntity(int min = 0, int max = 0)
        {
            return new ComparisonEntity
            {
                Minimum = min,
                Maximum = max
            };
        }

        [Test]
        public void RequiresValidationContext_IsTrue()
        {
            CompareValuesAttribute attribute = new CompareValuesAttribute(
                "property", ComparisonCriteria.EqualTo);

            Assert.That(attribute.RequiresValidationContext);
        }

        [Test]
        public void Constructor_NullOtherProperty_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new CompareValuesAttribute(null, ComparisonCriteria.EqualTo));
        }

        [Test]
        public void CompareValues_InvalidOtherProperty_ThrowsInvalidOperationException()
        {
            ComparisonEntity entity = CreateComparisonEntity();
            ValidationContext validationContext = new ValidationContext(entity) { MemberName = "Minimum" };
            CompareValuesAttribute attribute = new CompareValuesAttribute(
                "InvalidProperty", ComparisonCriteria.EqualTo);

            Assert.Throws<InvalidOperationException>(
                () => attribute.Validate(entity.Minimum, validationContext));
        }

        [Test]
        public void CompareValues_DifferentTypedProperties_ThrowsInvalidOperationException()
        {
            ComparisonEntity entity = CreateComparisonEntity();
            ValidationContext validationContext = new ValidationContext(entity) { MemberName = "Minimum" };
            CompareValuesAttribute attribute = new CompareValuesAttribute(
                "StringType", ComparisonCriteria.LessThan);

            // Act
            Assert.Throws<InvalidOperationException>(
                () => attribute.Validate(entity.Minimum, validationContext));
        }

        [Test]
        public void CompareValues_UncomparableProperties_IsInvalid()
        {
            ComparisonEntity entity = CreateComparisonEntity();
            ValidationContext validationContext = new ValidationContext(entity)
            {
                MemberName = "Uncomparable"
            };
            CompareValuesAttribute attribute = new CompareValuesAttribute(
                "OtherUncomparable", ComparisonCriteria.LessThan);

            ValidationResult result = attribute.GetValidationResult(entity.Uncomparable, validationContext);

            Assert.That(result.ErrorMessage, Is.StringContaining("IComparable"));
        }

        [TestCase(ComparisonCriteria.EqualTo, 1, 1)]
        [TestCase(ComparisonCriteria.NotEqualTo, 0, 1)]
        [TestCase(ComparisonCriteria.GreaterThan, 1, 0)]
        [TestCase(ComparisonCriteria.GreatThanOrEqualTo, 1, 1)]
        [TestCase(ComparisonCriteria.GreatThanOrEqualTo, 1, 0)]
        [TestCase(ComparisonCriteria.LessThan, 0, 1)]
        [TestCase(ComparisonCriteria.LessThanOrEqualTo, 1, 1)]
        [TestCase(ComparisonCriteria.LessThanOrEqualTo, 0, 1)]
        public void CompareValues_MinimumComparedToMaximumWithValidValues_IsValid(
            ComparisonCriteria criteria, int minimum, int maximum)
        {
            ComparisonEntity entity = CreateComparisonEntity(minimum, maximum);
            ValidationContext validationContext = new ValidationContext(entity) { MemberName = "Minimum" };
            CompareValuesAttribute attribute = new CompareValuesAttribute("Maximum", criteria);

            // Act
            ValidationResult result = attribute.GetValidationResult(entity.Minimum, validationContext);

            Assert.That(result, Is.EqualTo(ValidationResult.Success));
        }

        [TestCase(ComparisonCriteria.EqualTo, 1, 0)]
        [TestCase(ComparisonCriteria.NotEqualTo, 1, 1)]
        [TestCase(ComparisonCriteria.GreaterThan, 1, 1)]
        [TestCase(ComparisonCriteria.GreatThanOrEqualTo, 0, 1)]
        [TestCase(ComparisonCriteria.LessThan, 1, 0)]
        [TestCase(ComparisonCriteria.LessThanOrEqualTo, 1, 0)]
        public void CompareValues_MinimumComparedToMaximumWithInvalidValues_IsInvalid(
            ComparisonCriteria criteria, int minimum, int maximum)
        {
            ComparisonEntity entity = CreateComparisonEntity(minimum, maximum);
            ValidationContext validationContext = new ValidationContext(entity) { MemberName = "Minimum" };
            CompareValuesAttribute attribute = new CompareValuesAttribute("Maximum", criteria);

            // Act
            ValidationResult result = attribute.GetValidationResult(entity.Minimum, validationContext);

            Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
        }

        [Test]
        public void GetValidationResult_InvalidValues_UsesDisplayNameAttributeValueFromOtherProperty()
        {
            ComparisonEntity entity = CreateComparisonEntity(1, 0);
            ValidationContext validationContext = new ValidationContext(entity)
            {
                MemberName = "MinDisplayName"
            };
            CompareValuesAttribute attribute = new CompareValuesAttribute(
                "Maximum", ComparisonCriteria.LessThan);

            // Act
            ValidationResult result = attribute.GetValidationResult(entity.Minimum, validationContext);

            Assert.That(result.ErrorMessage, Is.StringContaining("MaxDisplay"));
        }

        [Test]
        public void GetValidationResult_InvalidValues_UsesDisplayAttributeNameValueFromOtherProperty()
        {
            ComparisonEntity entity = CreateComparisonEntity(1, 0);
            ValidationContext validationContext = new ValidationContext(entity)
            {
                MemberName = "MaxDisplay"
            };
            CompareValuesAttribute attribute = new CompareValuesAttribute(
                "Minimum", ComparisonCriteria.GreaterThan);

            // Act
            ValidationResult result = attribute.GetValidationResult(entity.Minimum, validationContext);

            Assert.That(result.ErrorMessage, Is.StringContaining("MinDisplayName"));
        }

        [Test]
        public void GetValidationResult_InvalidValuesWithoutDisplayName_UsesNameOfOtherProperty()
        {
            ComparisonEntity entity = CreateComparisonEntity();
            entity.DefaultToNull = "";
            entity.StringType = "";
            ValidationContext validationContext = new ValidationContext(entity)
            {
                MemberName = "StringType"
            };
            CompareValuesAttribute attribute = new CompareValuesAttribute(
                "DefaultToNull", ComparisonCriteria.LessThan);

            // Act
            ValidationResult result = attribute.GetValidationResult(entity.StringType, validationContext);

            Assert.That(result.ErrorMessage, Is.StringContaining("DefaultToNull"));
        }

        [Test]
        public void GetValidationResult_InvalidValues_UsesDescriptionOfComparisonCriteria()
        {
            ComparisonEntity entity = CreateComparisonEntity(1, 0);
            ValidationContext validationContext = new ValidationContext(entity)
            {
                MemberName = "Minimum"
            };
            CompareValuesAttribute attribute = new CompareValuesAttribute(
                "Maximum", ComparisonCriteria.LessThan);

            // Act
            ValidationResult result = attribute.GetValidationResult(entity.Minimum, validationContext);

            Assert.That(result.ErrorMessage, Is.StringContaining("<"));
        }

        [TestCase(ComparisonCriteria.EqualTo)]
        [TestCase(ComparisonCriteria.GreaterThan)]
        [TestCase(ComparisonCriteria.GreatThanOrEqualTo)]
        [TestCase(ComparisonCriteria.LessThan)]
        [TestCase(ComparisonCriteria.LessThanOrEqualTo)]
        public void GetValidationResult_OneNullValue_IsInvalid(ComparisonCriteria criteria)
        {
            ComparisonEntity entity = CreateComparisonEntity(min: 1);
            ValidationContext validationContext = new ValidationContext(entity) { MemberName = "Minimum" };
            CompareValuesAttribute attribute = new CompareValuesAttribute("Maximum", criteria);

            // Act
            ValidationResult result = attribute.GetValidationResult(null, validationContext);

            Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
        }

        [Test]
        public void GetValidationResult_OneNullValueWithNotEqualAsCriteria_IsValid()
        {
            ComparisonEntity entity = CreateComparisonEntity(min: 1);
            ValidationContext validationContext = new ValidationContext(entity) { MemberName = "Minimum" };
            CompareValuesAttribute attribute = new CompareValuesAttribute(
                "Maximum", ComparisonCriteria.NotEqualTo);

            // Act
            ValidationResult result = attribute.GetValidationResult(null, validationContext);

            Assert.That(result, Is.EqualTo(ValidationResult.Success));
        }

        [Test]
        public void GetValidationResult_BothNull_IsValid()
        {
            ComparisonEntity entity = CreateComparisonEntity();
            ValidationContext validationContext = new ValidationContext(entity)
            {
                MemberName = "StringType"
            };
            CompareValuesAttribute attribute = new CompareValuesAttribute(
                "DefaultToNull", ComparisonCriteria.LessThan);

            // Act
            ValidationResult result = attribute.GetValidationResult(entity.StringType, validationContext);

            Assert.That(result, Is.EqualTo(ValidationResult.Success));
        }
    }
}