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

        /// <summary>
        ///     Instantiates a new instance of NotEnoughInventoryException with the provided arguments
        /// </summary>
        /// <param name="message">The exception message</param>
        /// <param name="product">The product that was too low on/out of stock</param>
        public NotEnoughInventoryException(string message, Product product) : base(message)
        {
            Product = product;
        }
    }
}