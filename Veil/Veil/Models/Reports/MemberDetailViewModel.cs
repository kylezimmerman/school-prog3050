using System;
using System.Collections.Generic;
using System.Linq;
using Veil.DataModels.Models;

namespace Veil.Models.Reports
{
    public class MemberDetailViewModel : DateFilteredListViewModel<MemberOrderViewModel>
    {
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public int WishlistCount { get; set; }
        public int FriendCount { get; set; }
        public ICollection<Platform> FavoritePlatforms { get; set; }
        public ICollection<Tag> FavoriteTags { get; set; }
        public string PlatformString => string.Join(", ", FavoritePlatforms.Select(p => p.PlatformCode));
        public string TagString => string.Join(", ", FavoriteTags.Select(t => t.Name));
        public int OrderCount => Items.Count(o => o.OrderStatus == OrderStatus.Processed);
        public int TotalQuantity => Items.Where(o => o.OrderStatus == OrderStatus.Processed).Sum(o => o.Quantity);
        public decimal ItemsSum => Items.Where(o => o.OrderStatus == OrderStatus.Processed).Sum(o => o.Subtotal);
        public decimal Total => Items.Where(o => o.OrderStatus == OrderStatus.Processed).Sum(o => o.OrderTotal);
    }

    public class MemberOrderViewModel
    {
        public long OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public decimal OrderTotal { get; set; }
    }
}