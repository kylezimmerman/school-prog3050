using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class GameListViewModel
    {
        public IEnumerable<Game> Games { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public int StartPage => Math.Max(1, CurrentPage - 2);
        public int EndPage => Math.Min(TotalPages, StartPage + 8);
    }
}