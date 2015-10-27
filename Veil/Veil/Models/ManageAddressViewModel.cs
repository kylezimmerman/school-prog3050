using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class ManageAddressViewModel
    {
        public string CountryCode { get; set; }

        public string ProvinceCode { get; set; }

        public IList<Country> Countries { get; set; }
    }
}