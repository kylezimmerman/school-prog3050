/* CountryExtensions.cs
 * Purpose: Extension methods for getting a select list of countries and provinces
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.27: Created
 */ 

using System.Collections.Generic;
using System.Web.Mvc;
using Veil.DataModels.Models;

namespace Veil.Extensions
{
    /// <summary>
    ///     Contains extension methods for <see cref="Country"/> and <see cref="Province"/>
    /// </summary>
    public static class CountryExtensions
    {
        /// <summary>
        ///     Extension method for generating <see cref="SelectListItem"/>s out of all the
        ///     passed countries' provinces
        /// </summary>
        /// <param name="countries">
        ///     The <see cref="IEnumerable{T}"/> of <see cref="Country"/> to generate province
        ///     SelectListItems for
        /// </param>
        /// <returns>
        ///     <see cref="IEnumerable{T}"/> of <see cref="SelectListItem"/> for all the provinces
        /// </returns>
        public static IEnumerable<SelectListItem> GetProvinceItems(this IEnumerable<Country> countries)
        {
            foreach (Country country in countries)
            {
                var currentGroup = new SelectListGroup
                {
                    Name = country.CountryName
                };

                foreach (var province in country.Provinces)
                {
                    yield return new SelectListItem
                    {
                        Text = province.Name,
                        Value = province.ProvinceCode,
                        Group = currentGroup
                    };
                }
            }
        }

        /// <summary>
        ///     Extension method for converting countries into <see cref="SelectListItem"/>s
        /// </summary>
        /// <param name="countries">
        ///     The <see cref="IEnumerable{T}"/> of <see cref="Country"/> to generate
        ///     SelectListItems for
        /// </param>
        /// <returns>
        ///     <see cref="IEnumerable{T}"/> of <see cref="SelectListItem"/> for all the countries
        /// </returns>
        public static IEnumerable<SelectListItem> CountryItems(this IEnumerable<Country> countries)
        {
            return new SelectList(countries, nameof(Country.CountryCode), nameof(Country.CountryName));
        }
    }
}