using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veil.DataModels.Models;

namespace Veil.Models.Reports
{
    public class GameDetailRowViewModel
    {
        public GameProduct GameProduct { get; set; }
        public int NewQuantity { get; set; }
        public decimal NewSales { get; set; }
        public int UsedQuantity { get; set; }
        public decimal UsedSales { get; set; }
        public int TotalQuantity => (NewQuantity + UsedQuantity);
        public decimal TotalSales => (NewSales + UsedSales);
    }

    public class GameDetailViewModel : DateFilteredListViewModel<GameDetailRowViewModel>
    {
        public Game Game { get; set; }
        public int TotalNewQuantity => Items.Sum(i => i.NewQuantity);
        public decimal TotalNewSales => Items.Sum(i => i.NewSales);
        public int TotalUsedQuantity => Items.Sum(i => i.UsedQuantity);
        public decimal TotalUsedSales => Items.Sum(i => i.UsedSales);
        public int TotalQuantity => (TotalNewQuantity + TotalUsedQuantity);
        public decimal TotalSales => (TotalNewSales + TotalUsedSales);
    }
}