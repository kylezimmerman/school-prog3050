/* IVeilDataAccess.cs
 * Purpose: Interface for database services for the application
 * 
 * Revision History:
 *      Drew Matheson, 2015.09.29: Created
 */

using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;

namespace Veil.DataAccess.Interfaces
{
    public interface IVeilDataAccess : IDisposable
    {
        DbSet<Cart> Carts { get; }
        DbSet<Company> Companies { get; }
        DbSet<Country> Countries { get; }
        DbSet<Department> Departments { get; }
        DbSet<DownloadGameProduct> DownloadGameProducts { get; }
        DbSet<Employee> Employees { get; }
        DbSet<ESRBContentDescriptor> ESRBContentDescriptors { get; }
        DbSet<ESRBRating> ESRBRatings { get; }
        DbSet<Event> Events { get; }
        DbSet<Friendship> Friendships { get; }
        DbSet<Game> Games { get; }
        DbSet<GameProduct> GameProducts { get; }
        DbSet<GameReview> GameReviews { get; }
        DbSet<Location> Locations { get; }
        DbSet<LocationType> LocationTypes { get; }
        DbSet<Member> Members { get; }
        DbSet<MemberAddress> MemberAddresses { get; }
        DbSet<PhysicalGameProduct> PhysicalGameProducts { get; }
        DbSet<Platform> Platforms { get; }
        DbSet<ProductLocationInventory> ProductLocationInventories { get; }
        DbSet<Province> Provinces { get; }
        DbSet<Tag> Tags { get; }
        DbSet<WebOrder> WebOrders { get; }

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