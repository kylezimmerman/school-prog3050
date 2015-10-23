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
using EfEnumToLookup.LookupGenerator;
using JetBrains.Annotations;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Microsoft.AspNet.Identity.EntityFramework;
using Veil.DataAccess.EntityConfigurations;
using Veil.DataModels.Models.Identity;

namespace Veil.DataAccess
{

    public class VeilDataContext : IdentityDbContext<User, GuidIdentityRole, Guid, GuidIdentityUserLogin, GuidIdentityUserRole, GuidIdentityUserClaim>, IVeilDataAccess
    {
        // NOTE: If you change either of this value, the Down() in the AddPhysicalGameProductSkuSequence
        // migration will not remove the old-named sequence
        internal const string PHYSICAL_GAME_PRODUCT_SKU_SEQUENCE_NAME = "PhysicalGameProductSkuSequence";

        // NOTE: If you change this value, no existing DB objects will be removed in migrations' Down()'s
        internal const string SCHEMA_NAME = "dbo";

        public DbSet<Cart> Carts { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<DownloadGameProduct> DownloadGameProducts { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<ESRBContentDescriptor> ESRBContentDescriptors { get; set; }
        public DbSet<ESRBRating> ESRBRatings { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<GameProduct> GameProducts { get; set; }
        public DbSet<GameReview> GameReviews { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<LocationType> LocationTypes { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<MemberAddress> MemberAddresses { get; set; }
        public DbSet<PhysicalGameProduct> PhysicalGameProducts { get; set; }
        public DbSet<Platform> Platforms { get; set; }
        public DbSet<ProductLocationInventory> ProductLocationInventories { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<WebOrder> WebOrders { get; set; }

        [UsedImplicitly]
        public VeilDataContext() : base("name=VeilDatabase")
        {
            /* Enum to Lookup Tables Setup */
            EnumToLookup enumToLookup = new EnumToLookup
            {
                TableNamePrefix = "",
                TableNameSuffix = "_Lookup",
                NameFieldLength = 64
            };

            enumToLookup.Apply(this);

            /* ASP.NET Identity Setup */
            RequireUniqueEmail = true;
        }

        public void MarkAsModified(Game game)
        {
            Entry(game).State = EntityState.Modified;
        }

        public long GetNextPhysicalGameProductSku()
        {
            // TODO: Add actual checking logic into this
            DbRawSqlQuery<long> result = Database.SqlQuery<long>($"SELECT NEXT VALUE FOR {PHYSICAL_GAME_PRODUCT_SKU_SEQUENCE_NAME};");

            long value = result.FirstOrDefault();

            return value;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>(); // Delete the one, cascade delete the many
            //modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>(); // Delete on either side cascade deletes the joining table

            // The specific EntityConfig chosen here was essentially random. We just needed something in the namespace
            modelBuilder.Configurations.AddFromAssembly(typeof(GameEntityConfig).Assembly);

            base.OnModelCreating(modelBuilder);

            // These must come after as Identity does its own initial config which we must override
            IdentityEntitiesConfig.Setup(modelBuilder);
            UserEntityConfig.Setup(modelBuilder);

        }
    }
}