/* NotEnoughInventoryException.cs
 * Purpose: Exception thrown when we don't have enough inventory for an un-restockable product
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.17: Created
 */ 

using System;
using Veil.DataModels.Models;

namespace Veil.Exceptions
{
    /// <summary>
    ///     Exception thrown when there isn't enough inventory on hand for a version of 
    ///     a <see cref="Veil.DataModels.Models.Product"/> which can't be restocked
    /// </summary>
    public class NotEnoughInventoryException : Exception
    {
        /// <summary>
        ///     The product there wasn't enough inventory for
        /// </summary>
        public Product Product { get; set; }

        public NotEnoughInventoryException(string message, Product product) : base(message)
        {
            Product = product;
        }
    }
}