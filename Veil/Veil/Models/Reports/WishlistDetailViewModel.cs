using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Veil.DataModels.Models;

namespace Veil.Models.Reports
{
    public class WishlistDetailGameViewModel
    {
        public Game Game { get; set; }
        public IEnumerable<WishlistDetailGameProductViewModel> GameProducts { get; set; }
        public int? WishlistCount { get; set; }
    }

    public class WishlistDetailGameProductViewModel
    {
        public GameProduct GameProduct { get; set; }
        public int? WishlistCount { get; set; }
    }
}