/* GameDetailViewModel.cs
 * Purpose: View models for the GameDetail report
 * 
 * Revision History:
 *      Justin Coschi, 2015.11.27: Created
 */ 

using System.Linq;
using Veil.DataModels.Models;

namespace Veil.Models.Reports
{
    /// <summary>
    ///     View model for a row in the GameDetail report
    /// </summary>
    public class GameDetailRowViewModel
    {
        /// <summary>
        ///     The game product for the row
        /// </summary>
        public GameProduct GameProduct { get; set; }

        /// <summary>
        ///     The number of new copies sold for the game product
        /// </summary>
        public int NewQuantity { get; set; }

        /// <summary>
        ///     The total dollar amount from the new copies sold
        /// </summary>
        public decimal NewSales { get; set; }

        /// <summary>
        ///     The number of used copies sold for the game product
        /// </summary>
        public int UsedQuantity { get; set; }

        /// <summary>
        ///     The total dollar amount from the used copies sold
        /// </summary>
        public decimal UsedSales { get; set; }

        /// <summary>
        ///     The total number of the copies sold
        /// </summary>
        public int TotalQuantity => (NewQuantity + UsedQuantity);

        /// <summary>
        ///     The total dollar amount of the copies sold
        /// </summary>
        public decimal TotalSales => (NewSales + UsedSales);
    }

    /// <summary>
    ///     View model for the GameDetail report
    /// </summary>
    public class GameDetailViewModel : DateFilteredListViewModel<GameDetailRowViewModel>
    {
        /// <summary>
        ///     The game the report is fort
        /// </summary>
        public Game Game { get; set; }

        /// <summary>
        ///     The total number of new copies sold across all SKUs
        /// </summary>
        public int TotalNewQuantity => Items.Sum(i => i.NewQuantity);

        /// <summary>
        ///     The total dollar amount from the new copies sold across all SKUs
        /// </summary>
        public decimal TotalNewSales => Items.Sum(i => i.NewSales);

        /// <summary>
        ///     The total number of used copies sold across all SKUs
        /// </summary>
        public int TotalUsedQuantity => Items.Sum(i => i.UsedQuantity);

        /// <summary>
        ///     The total dollar amount from the used copies sold across all SKUs
        /// </summary>
        public decimal TotalUsedSales => Items.Sum(i => i.UsedSales);

        /// <summary>
        ///     The total number of copies sold across all SKUs
        /// </summary>
        public int TotalQuantity => (TotalNewQuantity + TotalUsedQuantity);

        /// <summary>
        ///     The total dollar amount of the copies sold across all SKUs
        /// </summary>
        public decimal TotalSales => (TotalNewSales + TotalUsedSales);
    }
}