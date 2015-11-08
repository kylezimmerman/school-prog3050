using System;
using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class GameListViewModel
    {
        protected int currentPage = 1;

        public IEnumerable<Game> Games { get; set; }

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

        public int TotalPages { get; set; }

        public int StartPage => Math.Max(1, CurrentPage - 2);
        public int EndPage => Math.Min(TotalPages, StartPage + 8);
    }
}