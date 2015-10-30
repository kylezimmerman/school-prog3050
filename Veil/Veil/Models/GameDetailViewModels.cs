using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class GameDetailViewModels
    {
        public Game Game { get; set; }
        public DateTime EarliestRelease { get; set; }
    }
}