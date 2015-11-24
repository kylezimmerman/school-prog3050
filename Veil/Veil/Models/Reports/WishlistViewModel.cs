using System.Collections.Generic;
using System.Linq;
using Veil.DataModels.Models;

namespace Veil.Models.Reports
{
    public class WishlistViewModel
    {
        public IEnumerable<WishlistGameViewModel> Games { get; set; }
        public IEnumerable<WishlistPlatformViewModel> Platforms { get; set; }
        public int WishlistCount => Games.Sum(g => g.WishlistCount) ?? 0;
    }

    public class WishlistGameViewModel
    {
        public Game Game { get; set; }
        public IEnumerable<WishlistGamePlatformViewModel> Platforms { get; set; }
        public int? WishlistCount { get; set; }
    }

    public class WishlistGamePlatformViewModel
    {
        public Platform GamePlatform { get; set; }
        public int? WishlistCount { get; set; }
    }

    public class WishlistPlatformViewModel
    {
        public Platform Platform { get; set; }
        public int? WishlistCount { get; set; }
    }
}