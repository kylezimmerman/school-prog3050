// CompareValuesAttribute.cs
// Purpose: Allows validation of properties in comparison to another property
// 
// Revision History:
//      Drew Matheson, 2015.10.02: Created
// 

using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Veil.DataModels.Validation
{
    // Note: Modified version of 
    // http://cncrrnt.com/blog/index.php/2011/01/custom-validationattribute-for-comparing-properties/

    /// <summary>
    /// Specifies that the field must compare favourably with the named field. If objects to check are 
    /// not the same type, false will be return
    /// </summary>
    [AttributeUsage(AttributeTargets.Property |
        AttributeTargets.Field |
        AttributeTargets.Parameter,
        AllowMultiple = false)]
    public class CompareValuesAttribute : ValidationAttribute
    {
        public override bool RequiresValidationContext
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// The other property to compare to
        /// </summary>
        public string OtherProperty { get; set; }

        public string OtherPropertyDisplayName { get; private set; }

        /// <summary>
        /// The comparison criteria used for this instance
        /// </summary>
        public ComparisonCriteria Criteria { get; set; }

        /// <summary>
        /// Creates the attribute
        /// </summary>
        /// <param name="otherProperty">The other property to compare to</param>
        /// <param name="criteria">The <see cref="ComparisonCriteria"/> to use when comparing</param>
        /// <exception cref="ArgumentNullException"><paramref name="otherProperty"/> is <see langword="null" />.</exception>
        public CompareValuesAttribute(string otherProperty, ComparisonCriteria criteria)
            : base("{0} must be {1} {2}")
        {
            if (otherProperty == null)
            {
                throw new ArgumentNullException(nameof(otherProperty));
            }

            OtherProperty = otherProperty;
            Criteria = criteria;
        }

        /// <summary>
        /// Determines whether the specified value of the object is valid. For this to be the case, 
        /// the objects must be of the same type and satisfy the comparison criteria. Null values will 
        /// return false in all cases except when both objects are null. The objects will need to 
        /// implement IComparable for the GreaterThan, LessThan, GreatThanOrEqualTo, and LessThanOrEqualTo
        /// Enum values
        /// </summary>
        /// <param name="value">The value of the object to validate</param>
        /// <param name="validationContext">The validation context</param>
        /// <returns>A validation result if the object is invalid, null if the object is valid</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if otherProperty can't be found or
        ///     the properties aren't the same type
        /// </exception>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // The other property
            PropertyInfo otherPropertyInfo = validationContext.ObjectType.GetProperty(OtherProperty);

            if (otherPropertyInfo == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "The {0} property couldn't be found. " +
                            "The otherProperty value supplied to the constructor must be the name of a property.",
                        OtherProperty));
            }

            object otherValue = otherPropertyInfo.GetValue(validationContext.ObjectInstance, null);

            if (value == null && otherValue == null)
            {
                return ValidationResult.Success;
            }

            if (Criteria == ComparisonCriteria.EqualTo)
            {
                if (Equals(value, otherValue))
                {
                    return ValidationResult.Success;
                }
            }
            else if (Criteria == ComparisonCriteria.NotEqualTo)
            {
                if (!Equals(value, otherValue))
                {
                    return ValidationResult.Success;
                }
            }
            else if (value != null)
            {
                if (value.GetType() != otherPropertyInfo.PropertyType)
                {
                    SetOtherPropertyDisplayName(otherPropertyInfo);

                    throw new InvalidOperationException(
                        string.Format(
                            "The types of the properties {0} and {1} must be the same.",
                            validationContext.DisplayName,
                            OtherPropertyDisplayName));
                }

                // Check that the type being compared implements IComparable
                // Note: Both must be the same type so only one check is required
                if (!(value is IComparable))
                {
                    SetOtherPropertyDisplayName(otherPropertyInfo);

                    return
                        new ValidationResult(
                            string.Format(
                                "{0} and {1} must both implement IComparable",
                                validationContext.DisplayName, OtherProperty));
                }

                // Compare the objects
                var result = Comparer.Default.Compare(value, otherValue);

                if (Criteria == ComparisonCriteria.GreaterThan && result > 0)
                {
                    return ValidationResult.Success;
                }

                if (Criteria == ComparisonCriteria.LessThan && result < 0)
                {
                    return ValidationResult.Success;
                }

                if (Criteria == ComparisonCriteria.GreatThanOrEqualTo && result >= 0)
                {
                    return ValidationResult.Success;
                }

                if (Criteria == ComparisonCriteria.LessThanOrEqualTo && result <= 0)
                {
                    return ValidationResult.Success;
                }
            }

            // Got this far must mean the items don't meet the comparison criteria
            SetOtherPropertyDisplayName(otherPropertyInfo);

            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
        }

        /// <summary>
        /// Applies formatting to an error message.
        /// </summary>
        /// <param name="name">The name to include in the error message</param>
        /// <returns></returns>
        public override string FormatErrorMessage(string name)
        {
            return string.Format(
                CultureInfo.CurrentCulture, ErrorMessageString, name,
                GetCriteriaDescription(Criteria), OtherPropertyDisplayName ?? OtherProperty);
        }

        /// <summary>
        ///     Gets the description value for the supplied ComparisonCriteria value
        ///     or if it doesn't have a description, its string representation
        /// </summary>
        /// <param name="value">The <see cref="ComparisonCriteria"/> value</param>
        /// <returns>The values description</returns>
        private static string GetCriteriaDescription(ComparisonCriteria value)
        {
            DescriptionAttribute attribute =
                (DescriptionAttribute) value.GetType().GetField(value.ToString()).
                    GetCustomAttributes(typeof (DescriptionAttribute)).FirstOrDefault();

            if (attribute != null)
            {
                return attribute.Description;
            }

            return value.ToString();
        }

        /// <summary>
        ///     Sets the OtherPropertyDisplayName from display name attributes or if non exist,
        ///     the member name of the OtherProperty
        /// </summary>
        /// <param name="info"></param>
        private void SetOtherPropertyDisplayName(PropertyInfo info)
        {
            if (OtherPropertyDisplayName == null)
            {
                DisplayNameAttribute displayNameAttribute =
                    Attribute.GetCustomAttribute(info, typeof (DisplayNameAttribute)) as
                        DisplayNameAttribute;

                if (displayNameAttribute != null)
                {
                    OtherPropertyDisplayName = displayNameAttribute.DisplayName;
                }
                else
                {
                    DisplayAttribute displayAttribute =
                        Attribute.GetCustomAttribute(info, typeof (DisplayAttribute)) as DisplayAttribute;

                    if (displayAttribute != null)
                    {
                        OtherPropertyDisplayName = displayAttribute.GetName();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Indicates a comparison criteria used by the ComparisonCriteria attribute
    /// </summary>
    public enum ComparisonCriteria
    {
        /// <summary>
        /// Check if the values are equal
        /// </summary>
        [Description("=")]
        EqualTo,

        /// <summary>
        /// Check if the values are not equal
        /// </summary>
        [Description("!=")]
        NotEqualTo,

        /// <summary>
        /// Check if this value is greater than the supplied value
        /// </summary>
        [Description(">")]
        GreaterThan,

        /// <summary>
        /// Check if this value is less than the supplied value
        /// </summary>
        [Description("<")]
        LessThan,

        /// <summary>
        /// Check if this value is greater than or equal to the supplied value
        /// </summary>
        [Description(">=")]
        GreatThanOrEqualTo,

        /// <summary>
        /// Check if this value is less than or equal to the supplied value
        /// </summary>
        [Description("<=")]
        LessThanOrEqualTo
    }
}