/* GameListViewModel.cs
 * Purpose: View model for the Games list
 * 
 * Revision History:
 *      Kyle Zimmerman, 2015.1.06: Created
 */ 

using System;
using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Models
{
    /// <summary>
    ///     View model for the <see cref="Game"/>s list
    /// </summary>
    public class GameListViewModel
    {
        /// <summary>
        ///     Backing field for the current page number being displayed
        /// </summary>
        protected int currentPage = 1;

        /// <summary>
        ///     The games to be displayed
        /// </summary>
        public IEnumerable<Game> Games { get; set; }

        /// <summary>
        ///     The current page number being displayed
        ///     <br/>
        ///     If set to zero or less, it is set to 1
        /// </summary>
        public int CurrentPage
        {
            get
            {
                return currentPage;
            }
            set
            {
                currentPage = value < 1 ? 1 : value;
            }
        }

        /// <summary>
        ///     The total number of pages
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        ///     The starting page for the current page range
        /// </summary>
        public int StartPage => Math.Max(1, CurrentPage - 2);

        /// <summary>
        ///     The ending page for the current page range
        /// </summary>
        public int EndPage => Math.Min(TotalPages, StartPage + 8);
    }
}