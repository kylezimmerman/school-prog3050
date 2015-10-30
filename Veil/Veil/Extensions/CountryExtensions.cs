using System.Collections.Generic;
using System.Web.Mvc;
using Veil.DataModels.Models;

namespace Veil.Extensions
{
    public static class CountryExtensions {
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

        public static IEnumerable<SelectListItem> CountryItems(this IEnumerable<Country> countries)
        {
            return new SelectList(countries, nameof(Country.CountryCode), nameof(Country.CountryName));
        } 
    }
}