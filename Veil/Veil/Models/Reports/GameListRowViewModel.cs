/* GameListViewModel.cs
 * Purpose: View model for the a row of the game list report
 * 
 * Revision History:
 *      Justin Coschi, 2015.11.20: Created
 */ 

using Veil.DataModels.Models;

namespace Veil.Models.Reports
{
    /// <summary>
    ///     View model for a row of the Game List report
    /// </summary>
    public class GameListRowViewModel
    {
        /// <summary>
        ///     The game for the row
        /// </summary>
        public Game Game { get; set; }

        /// <summary>
        ///     The number of copies sold across all SKUs
        /// </summary>
        public int QuantitySold { get; set; }
    }
}