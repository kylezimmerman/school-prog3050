using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class HomePageViewModel
    {
        public IEnumerable<Game> ComingSoon { get; set; }
        public IEnumerable<Game> NewReleases { get; set; }
    }
}