/* ProductLocationInventory.cs
 * Purpose: A class for tracking product inventory levels at locations
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.03: Created
 */

using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// Inventory levels for a specific product at a specific location
    /// </summary>
    public class ProductLocationInventory
    {
        /// <summary>
        /// The Id of the product for this ProductLocationInventory
        /// </summary>
        [Key]
        public string ProductId { get; set; }

        /// <summary>
        /// The Id of the location this ProductLocationInventory
        /// </summary>
        [Key]
        public string LocationId { get; set; }

        /// <summary>
        /// Navigation property for the product 
        /// </summary>
        public virtual Product Product { get; set; }

        /// <summary>
        /// Navigation property for the location
        /// </summary>
        public virtual Location Location { get; set; }

        /// <summary>
        /// The new on hand quantity for this product and location
        /// </summary>
        public int NewOnHand { get; set; }

        /// <summary>
        /// The new on order quantity for this product and location
        /// </summary>
        public int NewOnOrder { get; set; }

        /// <summary>
        /// The used on hand quantity for this product and location
        /// </summary>
        public int UsedOnHand { get; set; }
    }
}