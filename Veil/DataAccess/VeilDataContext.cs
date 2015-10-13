/* VeilDataContext.cs
 * Purpose: Database context class for the application's database
 * 
 * Revision History:
 *      Drew Matheson, 2015.09.29: Created
 */

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration.Conventions;
using EfEnumToLookup.LookupGenerator;
using JetBrains.Annotations;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Microsoft.AspNet.Identity.EntityFramework;
using Veil.DataModels.Models.Identity;

namespace Veil.DataAccess
{

    public class VeilDataContext : IdentityDbContext<User, GuidIdentityRole, Guid, GuidIdentityUserLogin, GuidIdentityUserRole, GuidIdentityUserClaim>, IVeilDataAccess
    {
        [UsedImplicitly]
        public VeilDataContext() : base("name=VeilDatabase")
        {
            EnumToLookup enumToLookup = new EnumToLookup
            {
                TableNamePrefix = "",
                TableNameSuffix = "_Lookup",
                NameFieldLength = 64
            };

            enumToLookup.Apply(this);

            RequireUniqueEmail = true;
        }

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

        public void MarkAsModified(Game game)
        {
            Entry(game).State = EntityState.Modified;
        }

        protected void SetupUserModel(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * Id
             */
            modelBuilder.Entity<User>().
                HasKey(u => u.Id).
                Property(u => u.Id).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<User>().Property(u => u.Email).IsRequired();

            modelBuilder.Entity<User>().ToTable(nameof(User));
        }

        protected void SetupIdentityModels(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GuidIdentityRole>().ToTable("Role");
            modelBuilder.Entity<GuidIdentityUserClaim>().ToTable("UserClaim");
            modelBuilder.Entity<GuidIdentityUserLogin>().ToTable("UserLogin");
            modelBuilder.Entity<GuidIdentityUserRole>().ToTable("UserRole");
        }

        protected void SetupGameModel(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * Id
             */
            modelBuilder.Entity<Game>().
                HasKey(g => g.Id).
                Property(g => g.Id).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            /* Foreign Keys:
             *
             * ESRBRating: ESRBRatingId
             */

            modelBuilder.Entity<Game>().
                HasRequired(g => g.Rating).
                WithMany(r => r.Games).
                HasForeignKey(g => g.ESRBRatingId);

            /* Many to Many Relationships:
             *
             * Game <=> ESRBContentDescriptors
             * Game <=> Tags
             */

            modelBuilder.Entity<Game>().
                HasMany(g => g.ContentDescriptors).
                WithMany();
        }

        protected void SetupWebOrderModel(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * Id
             */
            modelBuilder.Entity<WebOrder>().
                HasKey(wo => wo.Id).
                Property(wo => wo.Id).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            /* Foreign keys:
             *
             * Member: MemberId
             * MemberAddress (ShippingAddress property): (ShippingAddressId, MemberId)
             * MemberCreditCard: (CreditCardNumber, MemberId)
             */

            modelBuilder.Entity<WebOrder>().
                HasRequired(wo => wo.Member).
                WithMany(m => m.WebOrders).
                HasForeignKey(wo => wo.MemberId);

            modelBuilder.Entity<WebOrder>().
                HasRequired(wo => wo.ShippingAddress).
                WithMany().
                HasForeignKey(wo => new { wo.ShippingAddressId, wo.MemberId });

            modelBuilder.Entity<WebOrder>().
                HasRequired(wo => wo.MemberCreditCard).
                WithMany().
                HasForeignKey(wo => new { wo.CreditCardNumber, wo.MemberId });

            modelBuilder.Entity<WebOrder>().
                HasMany(wo => wo.OrderItems).
                WithRequired().
                HasForeignKey(oi => oi.OrderId);
        }

        protected void SetupOrderItemModel(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * OrderId, ProductId
             */
            modelBuilder.Entity<OrderItem>().
                HasKey(ci => new { ci.OrderId, ci.ProductId });

            /* Foreign Key:
             *
             * Product: ProductId
             * WebOrder: OrderId (setup in SetupWebOrderModel as OrderItem doesn't have
             *                    a navigation property to the WebOrder)
             */
            modelBuilder.Entity<OrderItem>().
                HasRequired(ci => ci.Product).
                WithMany().
                HasForeignKey(ci => ci.ProductId);
        }

        protected void SetupProvinceModel(DbModelBuilder modelBuilder)
        {
            /* Primary key:
             * 
             * ProvinceCode, CountryCode
             */
            modelBuilder.Entity<Province>().
                HasKey(p => new { p.ProvinceCode, p.CountryCode });

            /* Foreign keys:
             *
             * Country: CountryCode
             */
            modelBuilder.Entity<Province>().
                HasRequired(p => p.Country).
                WithMany(c => c.Provinces).
                HasForeignKey(p => p.CountryCode);
        }

        protected void SetupMemberAddressModel(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * MemberId, AddressId
             */
            modelBuilder.Entity<MemberAddress>().
                HasKey(ma => new { ma.AddressId, ma.MemberId }).
                Property(ma => ma.AddressId).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            /* Foreign keys: 
             *
             * Province: (ProvinceCode, CountryCode)
             * Country: CountryCode
             * Member: MemberId
             */

            modelBuilder.Entity<MemberAddress>().
                HasRequired(ma => ma.Province).
                WithMany().
                HasForeignKey(ma => new { ma.ProvinceCode, ma.CountryCode });

            modelBuilder.Entity<MemberAddress>().
                HasRequired(ma => ma.Country).
                WithMany().
                HasForeignKey(ma => ma.CountryCode);

            modelBuilder.Entity<MemberAddress>().
                HasRequired(ma => ma.Member).
                WithMany(m => m.ShippingAddresses).
                HasForeignKey(ma => ma.MemberId).
                WillCascadeOnDelete(true); // TODO: Figure out what this cascade delete actually means
        }

        protected void SetupLocationModel(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * Id
             */
            modelBuilder.Entity<Location>().
                HasKey(l => l.Id).
                Property(l => l.Id).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            /* Foreign keys: 
             *
             * Province: (ProvinceCode, CountryCode)
             * Country: CountryCode
             * LocationType: LocationTypeName (No Navigation property)
             */

            modelBuilder.Entity<Location>().
                HasRequired(a => a.Province).
                WithMany().
                HasForeignKey(a => new { a.ProvinceCode, a.CountryCode });

            modelBuilder.Entity<Location>().
                HasRequired(a => a.Country).
                WithMany().
                HasForeignKey(a => a.CountryCode);

            modelBuilder.Entity<LocationType>().
                HasMany(lt => lt.Locations).
                WithRequired().
                HasForeignKey(l => l.LocationTypeName);
        }

        protected void SetupMemberModel(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * UserId (mapped as MemberId)
             */

            modelBuilder.Entity<Member>().
                HasKey(m => m.UserId).
                Property(m => m.UserId).
                HasColumnName("MemberId").
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            /* Foreign Keys:
             *
             * User: UserId
             */
            modelBuilder.Entity<Member>().
                HasRequired(m => m.UserAccount).
                WithOptional(au => au.Member);

            /* Many to Many relationships:
             *
             * Member <=> Platform
             * Member <=> Tag
             * Member <=> Product (Wishlist)
             * Member <=> Event
             */

            modelBuilder.Entity<Member>().
                HasMany(m => m.FavoritePlatforms).
                WithMany(p => p.MembersFavoritePlatform).
                Map(
                    manyToManyConfig =>
                        manyToManyConfig.ToTable("MemberFavoritePlatform"));

            modelBuilder.Entity<Member>().
                HasMany(m => m.FavoriteTags).
                WithMany(t => t.MemberFavoriteCategory).
                Map(
                    manyToManyConfig =>
                        manyToManyConfig.ToTable("MemberFavoriteTag"));

            modelBuilder.Entity<Member>().
                HasMany(m => m.Wishlist).
                WithMany().
                Map(
                    t =>
                        t.MapLeftKey("MemberId").
                        MapRightKey("ProductId").
                        ToTable("MemberWishlistItem"));

            modelBuilder.Entity<Member>().
                HasMany(m => m.RegisteredEvents).
                WithMany(e => e.RegisteredMembers).
                Map(
                    manyToManyConfig =>
                        manyToManyConfig.ToTable("MemberEventMembership"));

            modelBuilder.Entity<Member>().ToTable(nameof(Member));
        }

        protected void SetupEmployeeModel(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * UserId (mapped as EmployeeUserId)
             */

            modelBuilder.Entity<Employee>().
                HasKey(emp => emp.UserId).
                Property(emp => emp.UserId).
                HasColumnName("EmployeeUserId").
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            /* Foreign Keys:
             *
             * User: UserId
             * Location: StoreLocationId
             * Department: DepartmentId
             */

            modelBuilder.Entity<Employee>().
                HasRequired(emp => emp.UserAccount).
                WithOptional(au => au.Employee);

            modelBuilder.Entity<Employee>().
                HasRequired(emp => emp.StoreLocation).
                WithMany().
                HasForeignKey(emp => emp.StoreLocationId);

            modelBuilder.Entity<Employee>().
                HasRequired(emp => emp.Department).
                WithMany().
                HasForeignKey(emp => emp.DepartmentId);

            // Unique constraint on the employee's Id
            modelBuilder.Entity<Employee>().
                Property(emp => emp.EmployeeId).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity).
                HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute("Employee_IX_EmployeeId_UQ")
                        {
                            IsUnique = true
                        }));

            modelBuilder.Entity<Employee>().ToTable(nameof(Employee));
        }

        protected void SetupProductAndDerivedModels(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * Id
             */
            modelBuilder.Entity<Product>().
                HasKey(p => p.Id).
                Property(p => p.Id).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            /* Many to Many Relationships:
             *
             * Product <=> Tag
             */
            modelBuilder.Entity<Product>().
                HasMany(p => p.Tags).
                WithMany(t => t.TaggedProducts).
                Map(manyToManyConfig => manyToManyConfig.ToTable("ProductCategory"));

            /* Foreign keys:
             *
             * Platform: PlatformCode
             * Game: GameId
             * GameProducts: PublisherId
             * GameProducts: DeveloperId
             */

            modelBuilder.Entity<GameProduct>().
                HasRequired(gp => gp.Platform).
                WithMany(p => p.GameProducts).
                HasForeignKey(gp => gp.PlatformCode);

            modelBuilder.Entity<GameProduct>().
                HasRequired(gp => gp.Game).
                WithMany(g => g.GameSKUs).
                HasForeignKey(gp => gp.GameId);

            modelBuilder.Entity<GameProduct>().
                HasRequired(gp => gp.Developer).
                WithMany(d => d.DevelopedGameProducts).
                HasForeignKey(gp => gp.DeveloperId);

            modelBuilder.Entity<GameProduct>().
                HasRequired(gp => gp.Publisher).
                WithMany(p => p.PublishedGameProducts).
                HasForeignKey(gp => gp.PublisherId);

            /* PhysicalGameProduct Unique Constraints:
             *
             * InternalNewSKU 
             * InternalUsedSKU
             */

            modelBuilder.Entity<PhysicalGameProduct>().Property(pgp => pgp.InternalNewSKU).HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("PhysicalGameProduct_IX_InternalNewSKU_UQ")
                    {
                        IsUnique = true
                    }));

            modelBuilder.Entity<PhysicalGameProduct>().Property(pgp => pgp.InteralUsedSKU).HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("PhysicalGameProduct_IX_InternalUsedSKU_UQ")
                    {
                        IsUnique = true
                    }));

            // Table per type for Products
            modelBuilder.Entity<GameProduct>().ToTable(nameof(GameProduct));
            modelBuilder.Entity<PhysicalGameProduct>().ToTable(nameof(PhysicalGameProduct));
            modelBuilder.Entity<DownloadGameProduct>().ToTable(nameof(DownloadGameProduct));
        }

        protected void SetupCartModel(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * MemberId
             */
            modelBuilder.Entity<Cart>().
                HasKey(c => c.MemberId).
                Property(c => c.MemberId).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            /* Foreign Key:
             *
             * Member: MemberId
             */
            modelBuilder.Entity<Cart>().
                HasRequired(c => c.Member).
                WithOptional(m => m.Cart);

            modelBuilder.Entity<Cart>().
                HasMany(c => c.Items).
                WithRequired().
                HasForeignKey(ci => ci.MemberId);
        }

        protected void SetupCartItemModel(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * MemberId (acts as PK for the cart), ProductId
             */
            modelBuilder.Entity<CartItem>().
                HasKey(ci => new { ci.MemberId, ci.ProductId });

            /* Foreign Keys:
             *
             * Product: ProductId
             * Cart: MemberId (setup in SetupCartModel)
             */
            modelBuilder.Entity<CartItem>().
                HasRequired(ci => ci.Product).
                WithMany().
                HasForeignKey(ci => ci.ProductId);
        }

        protected void SetupReviewAndDerivedModels(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * MemberId, GameProductId
             */
            modelBuilder.Entity<GameReview>().
                HasKey(g => new { g.MemberId, g.ProductReviewedId });

            /* Foreign keys:
             *
             * GameProduct: GameProductId
             */

            modelBuilder.Entity<GameReview>().
                HasRequired(gr => gr.ProductReviewed).
                WithMany(g => g.Reviews).
                HasForeignKey(gr => gr.ProductReviewedId);

            modelBuilder.Entity<GameReview>().
                HasRequired(g => g.Member).
                WithMany().
                HasForeignKey(g => g.MemberId);

            modelBuilder.Entity<GameReview>().
                Map(
                    t =>
                        t.MapInheritedProperties().
                        ToTable(nameof(GameReview)));
        }

        protected void SetupProductLocationInventoryModel(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * LocationId, ProductId
             */
            modelBuilder.Entity<ProductLocationInventory>().
                HasKey(pli => new { pli.LocationId, pli.ProductId });

            /* Foreign keys:
             *
             * Product: ProductId
             * Location: LocationId
             */

            modelBuilder.Entity<ProductLocationInventory>().
                HasRequired(pli => pli.Product).
                WithMany(p => p.LocationInventories).
                HasForeignKey(pli => pli.ProductId);

            modelBuilder.Entity<ProductLocationInventory>().
                HasRequired(pli => pli.Location).
                WithMany().
                HasForeignKey(pli => pli.LocationId);
        }

        protected void SetupFriendshipModel(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * Member: ReceiverId
             * Member: RequesterId
             */
            modelBuilder.Entity<Friendship>().
                HasKey(f => new { f.ReceiverId, f.RequesterId });

            /* Foreign Keys:
             *
             * Member: RequesterId
             * Member: ReceiverId
             */
            modelBuilder.Entity<Friendship>().
                HasRequired(f => f.Requester).
                WithMany(m => m.RequestedFriendships).
                HasForeignKey(f => f.RequesterId);

            modelBuilder.Entity<Friendship>().
                HasRequired(f => f.Receiver).
                WithMany(m => m.ReceivedFriendships).
                HasForeignKey(f => f.ReceiverId);

            // NOTE: This doesn't work as expected. It causes RequesterId to become a 1..1 with Member
            // TODO: Add unique constraint on (RequesterId, ReceiverId). 
            // PK already adds unique for (ReceiverId, RequesterId)
            //const string FRIENDSHIP_UNIQUE_INDEX = "Friendship_IX_Friendship_UQ";

            /*modelBuilder.Entity<Friendship>().
                Property(f => f.RequesterId).
                HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute(FRIENDSHIP_UNIQUE_INDEX, 0)
                    {
                        IsUnique = true
                    }));

            modelBuilder.Entity<Friendship>().
                Property(f => f.RequesterId).
                HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute(FRIENDSHIP_UNIQUE_INDEX, 1)
                    {
                        IsUnique = true
                    }));*/
        }

        protected void SetupMemberCreditCardModel(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * CardNumber, MemberId
             */
            modelBuilder.Entity<MemberCreditCard>().
                HasKey(cc => new { cc.CardNumber, cc.MemberId }).
                ToTable(nameof(MemberCreditCard));

            /* Foreign Keys:
             *
             * Member: MemberId
             */
            modelBuilder.Entity<MemberCreditCard>().
               HasRequired(cc => cc.Member).
               WithMany(m => m.CreditCards).
               HasForeignKey(cc => cc.MemberId);

            /* Setup CreditCardBillingInfo to be in the same table */
            modelBuilder.Entity<CreditCardBillingInfo>().
                HasKey(bi => new { bi.CardNumber, bi.MemberId });

            modelBuilder.Entity<MemberCreditCard>().
                HasRequired(cc => cc.BillingInfo).
                WithRequiredPrincipal();

            modelBuilder.Entity<CreditCardBillingInfo>().ToTable(nameof(MemberCreditCard));
        }

        protected void SetupCompanyModel(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>().
                Property(c => c.Id).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        }

        protected void SetupESRBRatingModel(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ESRBRating>().
                Property(er => er.RatingId).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>(); // Delete the one, cascade delete the many
            //modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>(); // Delete on either side cascade deletes the joining table

            base.OnModelCreating(modelBuilder);

            SetupUserModel(modelBuilder);
            SetupIdentityModels(modelBuilder);
            SetupGameModel(modelBuilder);
            SetupProvinceModel(modelBuilder);
            SetupEmployeeModel(modelBuilder);
            SetupMemberModel(modelBuilder);
            SetupMemberCreditCardModel(modelBuilder);
            SetupMemberAddressModel(modelBuilder);
            SetupLocationModel(modelBuilder);
            SetupCartModel(modelBuilder);
            SetupCartItemModel(modelBuilder);
            SetupWebOrderModel(modelBuilder);
            SetupOrderItemModel(modelBuilder);
            SetupProductAndDerivedModels(modelBuilder);
            SetupReviewAndDerivedModels(modelBuilder);
            SetupProductLocationInventoryModel(modelBuilder);
            SetupFriendshipModel(modelBuilder);
            SetupCompanyModel(modelBuilder);
            SetupESRBRatingModel(modelBuilder);
        }
    }
}