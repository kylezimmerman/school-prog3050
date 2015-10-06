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
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using JetBrains.Annotations;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;

namespace Veil.DataAccess
{
    public class VeilDataContext : DbContext, IVeilDataAccess
    {
        [UsedImplicitly]
        public VeilDataContext() : base("name=VeilDatabase") { }

        public IDbSet<Cart> Carts { get; set; }
        public IDbSet<Company> Companies { get; set; }
        public IDbSet<Country> Countries { get; set; }
        public IDbSet<Department> Departments { get; set; }
        public IDbSet<DownloadGameProduct> DownloadGameProducts { get; set; }
        public IDbSet<Employee> Employees { get; set; }
        public IDbSet<ESRBContentDescriptor> ESRBContentDescriptors { get; set; }
        public IDbSet<ESRBRating> ESRBRatings { get; set; }
        public IDbSet<Event> Events { get; set; }
        public IDbSet<Friendship> Friendships { get; set; }
        public IDbSet<Game> Games { get; set; }
        public IDbSet<GameProduct> GameProducts { get; set; } // TODO: Do we want this for making a list of all product SKUs regardless of if they are DL or physical?
        public IDbSet<GameReview> GameReviews { get; set; }
        public IDbSet<Location> Locations { get; set; }
        public IDbSet<LocationType> LocationTypes { get; set; }
        public IDbSet<Member> Members { get; set; }
        public IDbSet<MemberAddress> MemberAddresses { get; set; }
        public IDbSet<PhysicalGameProduct> PhysicalGameProducts { get; set; }
        public IDbSet<Platform> Platforms { get; set; }
        public IDbSet<Product> Products { get; set; } // TODO: Do we want this?
        public IDbSet<ProductLocationInventory> ProductLocationInventories { get; set; }
        public IDbSet<Province> Provinces { get; set; }
        //public IDbSet<Review> Reviews { get; set; } // TODO: Do we want this?
        public IDbSet<Tag> Tags { get; set; }
        public IDbSet<WebOrder> WebOrders { get; set; }

        protected void SetupGameModel(DbModelBuilder modelBuilder)
        {
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

            modelBuilder.Entity<Game>().
                HasMany(g => g.GameCategories).
                WithMany(t => t.TaggedGames).
                Map(manyToManyConfig => manyToManyConfig.ToTable("GameCategory"));
        }

        protected void SetupWebOrderModel(DbModelBuilder modelBuilder)
        {
            /* Foreign keys:
             *
             * Member: MemberId
             * MemberAddress (ShippingAddress property): (ShippingAddressId, MemberId)
             * MemberCreditCard: (CreditCardNumber, MemberId)
             */

            modelBuilder.Entity<WebOrder>().
                HasRequired(wo => wo.Member).
                WithMany().
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

            // TODO: Remove if EF is setup to generate Ids by default
            /*modelBuilder.Entity<WebOrder>().
                Property(wo => wo.Id).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);*/
        }

        protected void SetupOrderItemModel(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * OrderId, ProductId
             */
            modelBuilder.Entity<OrderItem>().HasKey(ci => new { ci.OrderId, ci.ProductId });

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
                HasKey(ma => new { ma.AddressId, ma.MemberId });

            /* Foreign keys: 
             *
             * Province: (ProvinceCode, CountryCode)
             * Country: CountryCode
             * Member: MemberId
             */

            modelBuilder.Entity<MemberAddress>().
                HasRequired(a => a.Province).
                WithMany().
                HasForeignKey(a => new { a.ProvinceCode, a.CountryCode });

            modelBuilder.Entity<MemberAddress>().
                HasRequired(a => a.Country).
                WithMany().
                HasForeignKey(a => a.CountryCode);

            modelBuilder.Entity<MemberAddress>().
                HasRequired(ma => ma.Member).
                WithMany(m => m.ShippingAddresses).
                HasForeignKey(ma => ma.MemberId).
                WillCascadeOnDelete(true);
        }

        protected void SetupLocationModel(DbModelBuilder modelBuilder)
        {
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

        protected void SetupPersonAndDerivedModels(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * PersonId (mapped as MemberId)
             */

            modelBuilder.Entity<Member>().
                HasKey(m => m.PersonId).
                Property(m => m.PersonId).
                HasColumnName("MemberId");

            /* Foreign Keys:
             *
             * Cart: PersonId (mapped as MemberId and setup in SetupCartModel)
             */

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
                WithMany(). // TODO: Do we want Product to have a collection of members with the product on with wishlist?
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

            /* Foreign Keys:
             *
             * Location: StoreLocationId
             * Department: DepartmentId
             */

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
                        new IndexAttribute("IX_EmployeeId")
                        {
                            IsUnique = true
                        }));

            // Table Per Concrete Type:
            // Member
            // Employee
            // TODO: Figure out how this will work with Identity
            modelBuilder.Entity<Member>().
                Map(
                    t =>
                        t.MapInheritedProperties().
                        ToTable(nameof(Member)));

            modelBuilder.Entity<Employee>().
                Map(
                    t =>
                        t.MapInheritedProperties().
                        ToTable(nameof(Employee)));
        }

        protected void SetupProductAndDerivedModels(DbModelBuilder modelBuilder)
        {
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
            /*modelBuilder.Entity<Review>().
                HasRequired(r => r.Member).
                WithMany().
                HasForeignKey(r => r.MemberId);*/

            // TODO: GameReview -> Game Product and/or Review -> Product with a collection navigation property on the review

            /* Primary Key:
             *
             * MemberId, GameProductId
             */
            modelBuilder.Entity<GameReview>().
                HasKey(g => new { g.MemberId, g.GameProductId });

            /* Foreign keys:
             *
             * GameProduct: GameProductId
             */

            modelBuilder.Entity<GameReview>().
                HasRequired(gr => gr.GameProduct).
                WithMany().
                HasForeignKey(gr => gr.GameProductId);

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
                WithMany(). // TODO: Do we want to be able to get a locations inventory levels from the location?
                HasForeignKey(pli => pli.LocationId);
        }

        protected void SetupFriendshipModel(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * Member: ReceiverId
             * Member: RequesterId
             */
            modelBuilder.Entity<Friendship>().HasKey(f => new { f.ReceiverId, f.RequesterId });

            // Add unique constraint on (RequesterId, ReceiverId). 
            // PK already adds unique for (ReceiverId, RequesterId)
            const string FRIENDSHIP_UNIQUE_INDEX = "IX_Friendship_UQ";

            modelBuilder.Entity<Friendship>().
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
                    }));
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

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // TODO: Missing navigation properties we will want:
            // Product -> Review and/or GameProduct -> Review

            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>(); // Delete the one, cascade delete the many
            //modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>(); // Delete on either side cascade deletes the joining table

            SetupGameModel(modelBuilder);
            SetupProvinceModel(modelBuilder);
            SetupPersonAndDerivedModels(modelBuilder);
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

            base.OnModelCreating(modelBuilder);
        }
    }
}