/* IVeilDataAccess.cs
 * Purpose: Interface for database services for the application
 * 
 * Revision History:
 *      Drew Matheson, 2015.09.29: Created
 */ 

using System.Data.Entity;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;

namespace Veil.DataAccess.Interfaces
{
    public interface IVeilDataAccess
    {
        IDbSet<Cart> Carts { get; }
        IDbSet<Company> Companies { get; }
        IDbSet<Country> Countries { get; }
        IDbSet<Department> Departments { get; }
        IDbSet<DownloadGameProduct> DownloadGameProducts { get; }
        IDbSet<Employee> Employees { get; }
        IDbSet<ESRBContentDescriptor> ESRBContentDescriptors { get; }
        IDbSet<ESRBRating> ESRBRatings { get; }
        IDbSet<Event> Events { get; }
        IDbSet<Friendship> Friendships { get; }
        IDbSet<Game> Games { get; }
        IDbSet<GameProduct> GameProducts { get; }
        IDbSet<GameReview> GameReviews { get; }
        IDbSet<Location> Locations { get; }
        IDbSet<LocationType> LocationTypes { get; }
        IDbSet<Member> Members { get; }
        IDbSet<MemberAddress> MemberAddresses { get; }
        IDbSet<PhysicalGameProduct> PhysicalGameProducts { get; }
        IDbSet<Platform> Platforms { get; }
        IDbSet<ProductLocationInventory> ProductLocationInventories { get; }
        IDbSet<Province> Provinces { get; }
        IDbSet<Tag> Tags { get; }
        IDbSet<WebOrder> WebOrders { get; }

        /// <summary>
        /// IDbSet of Users
        /// </summary>
        IDbSet<User> Users { get; }

        /// <summary>
        /// IDbSet of Roles
        /// </summary>
        IDbSet<GuidIdentityRole> Roles { get; }
    }
}