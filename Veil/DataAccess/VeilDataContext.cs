/* VeilDataContext.cs
 * Purpose: Database context class for the application's database
 * 
 * Revision History:
 *      Drew Matheson, 2015.09.29: Created
 */

using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.AspNet.Identity;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Microsoft.AspNet.Identity.EntityFramework;
using Veil.DataAccess.EntityConfigurations;
using Veil.DataModels.Models.Identity;

namespace Veil.DataAccess
{
    /// <summary>
    ///     Implementation of IVeilDataAccess for use with SQL Server/Entity Framework
    /// </summary>
    public class VeilDataContext : IdentityDbContext<User, GuidIdentityRole, Guid, GuidIdentityUserLogin, GuidIdentityUserRole, GuidIdentityUserClaim>, IVeilDataAccess
    {
        // NOTE: If you change this value, the Down() in the AddPhysicalGameProductSkuSequence
        // migration will not remove the old-named sequence
        internal const string PHYSICAL_GAME_PRODUCT_SKU_SEQUENCE_NAME = "PhysicalGameProductSkuSequence";

        // NOTE: If you change this value, no existing DB objects will be removed in migrations' Down()'s
        internal const string SCHEMA_NAME = "dbo";

        // NOTE: Documentation for these can be found in IVeilDataAccess
        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.Carts"/>
        /// </summary>
        public DbSet<Cart> Carts { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.Companies"/>
        /// </summary>
        public DbSet<Company> Companies { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.Countries"/>
        /// </summary>
        public DbSet<Country> Countries { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.MemberCreditCards"/>
        /// </summary>
        public DbSet<MemberCreditCard> MemberCreditCards { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.Departments"/>
        /// </summary>
        public DbSet<Department> Departments { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.DownloadGameProducts"/>
        /// </summary>
        public DbSet<DownloadGameProduct> DownloadGameProducts { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.Employees"/>
        /// </summary>
        public DbSet<Employee> Employees { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.ESRBContentDescriptors"/>
        /// </summary>
        public DbSet<ESRBContentDescriptor> ESRBContentDescriptors { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.ESRBRatings"/>
        /// </summary>
        public DbSet<ESRBRating> ESRBRatings { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.Events"/>
        /// </summary>
        public DbSet<Event> Events { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.Friendships"/>
        /// </summary>
        public DbSet<Friendship> Friendships { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.Games"/>
        /// </summary>
        public DbSet<Game> Games { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.GameProducts"/>
        /// </summary>
        public DbSet<GameProduct> GameProducts { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.GameReviews"/>
        /// </summary>
        public DbSet<GameReview> GameReviews { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.Locations"/>
        /// </summary>
        public DbSet<Location> Locations { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.LocationTypes"/>
        /// </summary>
        public DbSet<LocationType> LocationTypes { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.Members"/>
        /// </summary>
        public DbSet<Member> Members { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.MemberAddresses"/>
        /// </summary>
        public DbSet<MemberAddress> MemberAddresses { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.PhysicalGameProducts"/>
        /// </summary>
        public DbSet<PhysicalGameProduct> PhysicalGameProducts { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.Platforms"/>
        /// </summary>
        public DbSet<Platform> Platforms { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.ProductLocationInventories"/>
        /// </summary>
        public DbSet<ProductLocationInventory> ProductLocationInventories { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.Products"/>
        /// </summary>
        public DbSet<Product> Products { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.Provinces"/>
        /// </summary>
        public DbSet<Province> Provinces { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.Tags"/>
        /// </summary>
        public DbSet<Tag> Tags { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.WebOrders"/>
        /// </summary>
        public DbSet<WebOrder> WebOrders { get; set; }

        /// <summary>
        ///     Implements <see cref="IVeilDataAccess.UserStore"/>
        /// </summary>
        public IUserStore<User, Guid> UserStore { get; }

        /// <summary>
        ///     Instantiates a new instance of VeilDataContext using the VeilDatabase connection string
        /// </summary>
        [UsedImplicitly]
        public VeilDataContext() : base("name=VeilDatabase")
        {
            /* ASP.NET Identity Setup */
            RequireUniqueEmail = true;

            UserStore = new UserStore<User, GuidIdentityRole, Guid, GuidIdentityUserLogin,
                                      GuidIdentityUserRole, GuidIdentityUserClaim>(this);
        }

        /// <summary>
        ///     Sets an Entity's <see cref="EntityState"/> to Modified
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the entity
        /// </typeparam>
        /// <param name="entity">
        ///     The entity itself
        /// </param>
        public void MarkAsModified<T>(T entity) where T : class
        {
            Entry(entity).State = EntityState.Modified;
        }

        /// <summary>
        ///     Gets the new SKU number for physical game products
        /// </summary>
        /// <returns></returns>
        public string GetNextPhysicalGameProductSku()
        {
            DbRawSqlQuery<long> result = Database.SqlQuery<long>($"SELECT NEXT VALUE FOR {PHYSICAL_GAME_PRODUCT_SKU_SEQUENCE_NAME};");

            long value = result.FirstOrDefault();

            string stringValue = value.ToString();

            return stringValue.PadLeft(12, '0');
        }

        /// <summary>
        ///     Sets up the Entity Framework model of the database
        /// </summary>
        /// <param name="modelBuilder">
        ///     The <see cref="DbModelBuilder"/> used to build the model
        /// </param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>(); // Delete the one, cascade delete the many
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>(); // Delete on either side cascade deletes the joining table
            modelBuilder.HasDefaultSchema(SCHEMA_NAME);

            // The specific EntityConfig chosen here was random. We just needed something in the namespace
            modelBuilder.Configurations.AddFromAssembly(typeof(ProductEntityConfig).Assembly);

            base.OnModelCreating(modelBuilder);

            // These must come after as Identity does its own initial config which we must override
            IdentityEntitiesConfig.Setup(modelBuilder);
            UserEntityConfig.Setup(modelBuilder);
        }

        /// <summary>
        ///     Disposes the IUserStore for the context and then calls base.Dispose
        /// </summary>
        /// <param name="disposing">
        ///     <c>true</c> to release both managed and unmanaged resources;
        ///     <c>false</c> to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UserStore.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}