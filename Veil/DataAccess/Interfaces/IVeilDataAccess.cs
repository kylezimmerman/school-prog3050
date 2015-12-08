/* IVeilDataAccess.cs
 * Purpose: Interface for database services for the application
 * 
 * Revision History:
 *      Drew Matheson, 2015.09.29: Created
 */

using System;
using System.Data.Entity;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;

namespace Veil.DataAccess.Interfaces
{
    /// <summary>
    ///     An enum of to provide friendly names for SQL Server Error Numbers
    /// </summary>
    /// <remarks>
    ///     Full List: https://technet.microsoft.com/en-us/library/cc645603%28v=sql.105%29.aspx
    /// </remarks>
    public enum SqlErrorNumbers
    {
        ConstraintViolation = 547
    }

    /// <summary>
    ///     Provides methods for interacting with for Veil's Data Access layer
    /// </summary>
    public interface IVeilDataAccess : IDisposable
    {
        /// <summary>
        ///     DbSet of Members' Carts
        /// </summary>
        DbSet<Cart> Carts { get; }

        /// <summary>
        ///     DbSet of Publishing and Development Companies
        /// </summary>
        DbSet<Company> Companies { get; }

        /// <summary>
        ///     DbSet of Countries
        /// </summary>
        DbSet<Country> Countries { get; }

        /// <summary>
        ///     DbSet of Member Credit Cards
        /// </summary>
        DbSet<MemberCreditCard> MemberCreditCards { get; }

            /// <summary>
        ///     DbSet of Departments
        /// </summary>
        DbSet<Department> Departments { get; }

        /// <summary>
        ///     DbSet of Downloadable Games
        /// </summary>
        DbSet<DownloadGameProduct> DownloadGameProducts { get; }

        /// <summary>
        ///     DbSet of Employees
        /// </summary>
        DbSet<Employee> Employees { get; }

        /// <summary>
        ///     DbSet of ESRB Content Descriptors
        /// </summary>
        DbSet<ESRBContentDescriptor> ESRBContentDescriptors { get; }

        /// <summary>
        ///     DbSet of ESRB Ratings
        /// </summary>
        DbSet<ESRBRating> ESRBRatings { get; }

        /// <summary>
        ///     DbSet of Events
        /// </summary>
        DbSet<Event> Events { get; }

        /// <summary>
        ///     DbSet of Members' Friendships
        /// </summary>
        DbSet<Friendship> Friendships { get; }

        /// <summary>
        ///     DbSet of Games
        /// </summary>
        DbSet<Game> Games { get; }

        /// <summary>
        ///     DbSet of Game Products
        /// </summary>
        DbSet<GameProduct> GameProducts { get; }

        /// <summary>
        ///     DbSet of Game Reviews
        /// </summary>
        DbSet<GameReview> GameReviews { get; }

        /// <summary>
        ///     DbSet of Locations
        /// </summary>
        DbSet<Location> Locations { get; }

        /// <summary>
        ///     DbSet of Types of Locations
        /// </summary>
        DbSet<LocationType> LocationTypes { get; }

        /// <summary>
        ///     DbSet of Members of the site. This is different than Users.
        /// </summary>
        DbSet<Member> Members { get; }

        /// <summary>
        ///     DbSet of Members' Addresses
        /// </summary>
        DbSet<MemberAddress> MemberAddresses { get; }

        /// <summary>
        ///     DbSet of Physical Game Products
        /// </summary>
        DbSet<PhysicalGameProduct> PhysicalGameProducts { get; }

        /// <summary>
        ///     DbSet of Platforms
        /// </summary>
        DbSet<Platform> Platforms { get; }

        /// <summary>
        ///     DbSet of Product Inventory at Locations
        /// </summary>
        DbSet<ProductLocationInventory> ProductLocationInventories { get; }

        /// <summary>
        ///     DbSet of Products
        /// </summary>
        DbSet<Product> Products { get; }

        /// <summary>
        ///     DbSet of Provinces
        /// </summary>
        DbSet<Province> Provinces { get; }

        /// <summary>
        ///     DbSet of Tags (aka Categories)
        /// </summary>
        DbSet<Tag> Tags { get; }

        /// <summary>
        ///     DbSet of Web Orders
        /// </summary>
        DbSet<WebOrder> WebOrders { get; }

        /// <summary>
        ///     IDbSet of Users
        /// </summary>
        IDbSet<User> Users { get; }

        /// <summary>
        ///     IDbSet of Roles
        /// </summary>
        IDbSet<GuidIdentityRole> Roles { get; }

        /// <summary>
        ///     The UserStore for Veil's Data Access Layer
        /// </summary>
        IUserStore<User, Guid> UserStore { get; }

        /// <summary>
        ///     Marks an entity state as modified
        /// </summary>
        /// <typeparam name="T">
        ///     The entity's type
        /// </typeparam>
        /// <param name="entity">
        ///     The entity to mark as modified
        /// </param>
        void MarkAsModified<T>(T entity) where T : class;

        /// <summary>
        ///     Asynchronously saves all changes made in this context to the underlying database.
        /// </summary>
        /// <returns>
        ///     A task that represents the asynchronous save operation.
        ///     The task result contains the number of state entries written to the underlying database.
        ///     This can include state entries for entities and/or relationships. 
        ///     Relationship state entries are created for many-to-many relationships and relationships 
        ///     where there is no foreign key property included in the entity class (often referred to as
        ///     independent associations).
        /// </returns>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.
        ///     Use 'await' to ensure that any asynchronous operations have completed before 
        ///     calling another method on this context.
        /// </remarks>
        /// <exception cref="System.Data.Entity.Infrastructure.DbUpdateException">
        ///     An error occurred sending updates to the database.
        /// </exception>
        /// <exception cref="System.Data.Entity.Infrastructure.DbUpdateConcurrencyException">
        ///     A database command did not affect the expected number of rows. 
        ///     This usually indicates an optimistic concurrency violation; that is, a row has been 
        ///     changed in the database since it was queried.
        /// </exception>
        /// <exception cref="System.Data.Entity.Validation.DbEntityValidationException">
        ///     The save was aborted because validation of entity property values failed.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     An attempt was made to use unsupported behavior such as executing multiple asynchronous 
        ///     commands concurrently on the same context instance.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The context or connection have been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     Some error occurred attempting to process entities in the context either before or 
        ///     after sending commands to the database.
        /// </exception>
        Task<int> SaveChangesAsync();

        /// <summary>
        ///     Gets the next SKU number for a Physical Game Product
        /// </summary>
        /// <returns>
        ///     The next SKU number
        /// </returns>
        string GetNextPhysicalGameProductSku();
    }
}