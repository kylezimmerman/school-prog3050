using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class ReviewViewModel
    {
        public Guid GameId { get; set; }
        public SelectList GameSKUSelectList { get; set; }
        public GameReview Review { get; set; }
    }
}