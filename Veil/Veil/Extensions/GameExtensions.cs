using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Veil.DataModels.Models;

namespace Veil.Extensions
{
    public static class GameExtensions
    {
        public static double? GetAverageRating(this Game game)
        {
            return game.GameSKUs.SelectMany(g => g.Reviews).Average(r => (double?)r.Rating);
        }
    }
}