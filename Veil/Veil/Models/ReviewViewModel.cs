/* ReviewViewModel.cs
 * Purpose: View model for creating a review for a game
 * 
 * Revision History:
 *      Kyle Zimmerman, 2015.11.13: Created
 */ 

using System;
using System.Web.Mvc;
using Veil.DataModels.Models;

namespace Veil.Models
{
    /// <summary>
    ///     View model for creating/editing a review for a <see cref="Game"/>
    /// </summary>
    public class ReviewViewModel
    {
        /// <summary>
        ///     The Id of the game being reviewed
        /// </summary>
        public Guid GameId { get; set; }

        /// <summary>
        ///     A select list containing all the SKUs for the Game
        /// </summary>
        public SelectList GameSKUSelectList { get; set; }

        /// <summary>
        ///     The <see cref="GameReview"/> itself
        /// </summary>
        public GameReview Review { get; set; }
    }
}