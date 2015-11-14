/* AddressViewModel.cs
 * Purpose: A view model for pages which list MemberAddresses and allow MemberAddress creation
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.27: Created
 */ 

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.ModelBinding;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.DataModels.Validation;

namespace Veil.Models
{
    /// <summary>
    ///     View model for listing and creating MemberAddresses
    /// </summary>
    public class AddressViewModel
    {
        private static readonly Regex postalCodeRegex = new Regex(
            ValidationRegex.POSTAL_CODE, RegexOptions.Compiled);

        private static readonly Regex zipCodeRegex = new Regex(
            ValidationRegex.ZIP_CODE, RegexOptions.Compiled);

        /// <summary>
        ///     The Id for this address entry.
        ///     <br/>
        ///     This is only used for Edit and Delete
        /// </summary>
        [BindNever]
        public virtual Guid Id { get; set; }

        /// <summary>
        /// The Address's street address, including apartment number
        /// </summary>
        [Required]
        [MaxLength(255)]
        [DisplayName("Street Address")]
        public string StreetAddress { get; set; }

        /// <summary>
        /// The Addresses optional post office box number
        /// </summary>
        [MaxLength(16)]
        [DisplayName("PO Box # (optional)")]
        public string POBoxNumber { get; set; }

        /// <summary>
        /// The Address's city
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string City { get; set; }

        /// <summary>
        /// The Address's postal or zip code
        /// </summary>
        [Required]
        [DataType(DataType.PostalCode)]
        [MaxLength(16)]
        [DisplayName("Postal Code")]
        [RegularExpression(ValidationRegex.POSTAL_ZIP_CODE,
            ErrorMessage =
                "Postal codes must be in the case insensitive format A0A 0A0 or A0A-0A0." +
                "\nZip codes must be in the format 12345, 12345-6789, or 12345 6789."
            )]
        public string PostalCode { get; set; }

        /// <summary>
        /// The province code for this Address's Province
        /// </summary>
        [Required]
        [DisplayName("Province/State")]
        public string ProvinceCode { get; set; }

        /// <summary>
        /// The country code for this Address's Country
        /// </summary>
        [Required]
        [DisplayName("Country")]
        public string CountryCode { get; set; }

        /// <summary>
        ///     List of countries
        /// </summary>
        [BindNever]
        public IList<Country> Countries { get; set; }

        /// <summary>
        ///     Select list items of the Member's Addresses
        /// </summary>
        [BindNever]
        public IEnumerable<SelectListItem> Addresses { get; set; }

        /// <summary>
        ///     Sets up the Addresses and Countries properties of the <see cref="AddressViewModel"/>
        /// </summary>
        /// <param name="db">
        ///     The <see cref="IVeilDataAccess"/> to fetch info from
        /// </param>
        /// <param name="memberId">
        ///     The Id of the Member to fetch info about
        /// </param>
        /// <returns>
        ///     A task to await
        /// </returns>
        public async Task SetupAddressesAndCountries(IVeilDataAccess db, Guid memberId)
        {
            var memberAddresses = await db.MemberAddresses.
                Where(ma => ma.MemberId == memberId).
                Select(
                    ma =>
                        new
                        {
                            ma.Id,
                            ma.Address.StreetAddress
                        }).
                ToListAsync();
            
            await SetupCountries(db);

            Addresses = new SelectList(
                memberAddresses, nameof(MemberAddress.Id), nameof(Address.StreetAddress));
        }

        /// <summary>
        ///     Sets up the Countries properties of the <see cref="AddressViewModel"/>
        /// </summary>
        /// <param name="db">
        ///     The <see cref="IVeilDataAccess"/> to fetch info from
        /// </param>
        /// <returns>
        ///     A task to await
        /// </returns>
        public async Task SetupCountries(IVeilDataAccess db)
        {
            Countries = await db.Countries.Include(c => c.Provinces).ToListAsync();
        }

        /// <summary>
        ///     Updates the PostalCode Model error to be more specific if possible
        /// </summary>
        /// <param name="modelState">
        ///     The ModelStateDictionary to be modified
        /// </param>
        public void UpdatePostalCodeModelError(System.Web.Mvc.ModelStateDictionary modelState)
        {
            if (!modelState.IsValidField(nameof(PostalCode)) &&
                !string.IsNullOrWhiteSpace(CountryCode))
            {
                // Remove the default validation message to provide a more specific one.
                modelState.Remove(nameof(PostalCode));

                modelState.AddModelError(
                    nameof(PostalCode),
                    CountryCode == "CA"
                        ? "You must provide a valid Canadian postal code in the format A0A 0A0"
                        : "You must provide a valid Zip Code in the format 12345 or 12345-6789");
            }
        }

        /// <summary>
        ///     Formats the PostalCode to be the standard persisted format
        /// </summary>
        public void FormatPostalCode()
        {
            if (CountryCode == "CA" && postalCodeRegex.IsMatch(PostalCode))
            {
                PostalCode = PostalCode.ToUpperInvariant();

                PostalCode = PostalCode.Length == 6
                    ? PostalCode.Insert(3, " ")
                    : PostalCode.Replace('-', ' ');
            }
            else if (CountryCode == "US" && zipCodeRegex.IsMatch(PostalCode))
            {
                PostalCode = PostalCode.ToUpperInvariant().Replace(' ', '-');
            }
        }

        /// <summary>
        ///     Maps the properties of this <see cref="AddressViewModel"/> to a 
        ///     new <see cref="Address"/> instance
        /// </summary>
        /// <returns>
        ///     The new <see cref="Address"/> instance with the values of this
        ///     <see cref="AddressViewModel"/>
        /// </returns>
        public Address MapToNewAddress()
        {
            return new Address
            {
                City = City,
                StreetAddress = StreetAddress,
                POBoxNumber = POBoxNumber,
                PostalCode = PostalCode
            };
        }
    }
}