// Location.cs
// Purpose: A class for business locations
// 
// Revision History:
//      Drew Matheson, 2015.10.03: Created
// 

using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// A business location
    /// </summary>
    public class Location : Address
    {
        /// <summary>
        /// The Location's sequential location/store number
        /// </summary>
        public int LocationNumber { get; set; }

        /// <summary>
        /// The location type name for this Location's type
        /// </summary>
        [Required]
        public string LocationTypeName { get; set; }

        /// <summary>
        /// The Location's site name
        /// </summary>
        public string SiteName { get; set; }

        /// <summary>
        /// The Location's phone number
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// The Location's fax number
        /// </summary>
        public string FaxNumber { get; set; }

        /// <summary>
        /// The Location's toll free number.
        /// This will be null if the location doesn't have a toll free number
        /// </summary>
        public string TollFreeNumber { get; set; }
    }
}