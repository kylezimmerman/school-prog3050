using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using Veil.DataAccess.Interfaces;
using Veil.Models;

namespace Veil.DataAccess
{
    public class VeilDataContext : DbContext, IVeilDataAccess
    {
        public VeilDataContext() : base("name=VeilDatabase") { }

        public IDbSet<Member> Members { get; set; }
        public IDbSet<Employee> Employees { get; set; }
        public IDbSet<Game> Games { get; set; }
        public IDbSet<WebOrder> WebOrders { get; set; } 

        protected void SetupCompanyModel(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>().
                HasMany(p => p.GameProducts).
                WithRequired(gp => gp.Developer).
                HasForeignKey(gp => gp.DeveloperId).
                WillCascadeOnDelete(false);

            modelBuilder.Entity<Company>().
                HasMany(p => p.GameProducts).
                WithRequired(gp => gp.Publisher).
                HasForeignKey(gp => gp.PublisherId).
                WillCascadeOnDelete(false);

/*            modelBuilder.Entity<GameProduct>().
                HasRequired(gp => gp.Developer).
                WithMany(c => c.GameProducts).
                HasForeignKey(gp => gp.DeveloperId).
                WillCascadeOnDelete(true);

            modelBuilder.Entity<GameProduct>().
                HasRequired(c => c.Publisher).
                WithMany(c => c.GameProducts).
                HasForeignKey(gp => gp.PublisherId).
                WillCascadeOnDelete(true);*/
        }

        protected void SetupGameModel(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Game>().
                HasMany(g => g.GameSKUs).
                WithRequired(gp => gp.Game).
                HasForeignKey(gp => gp.GameId);

            modelBuilder.Entity<Game>().
                HasRequired(g => g.Rating).
                WithMany(r => r.Games).
                HasForeignKey(g => g.ESRBRatingId);

            modelBuilder.Entity<Game>().
                HasMany(g => g.ContentDescriptors).
                WithOptional();
        }

        protected void SetupWebOrderModel(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WebOrder>().
                HasMany(wo => wo.OrderItems).
                WithRequired().
                HasForeignKey(oi => oi.ProductId).
                WillCascadeOnDelete(false);

            modelBuilder.Entity<WebOrder>().
                Property(wo => wo.Id).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        }

        protected void SetupOrderItemModel(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderItem>().
                HasRequired(ci => ci.Product).
                WithMany().
                HasForeignKey(ci => ci.ProductId).
                WillCascadeOnDelete(false);

            modelBuilder.Entity<OrderItem>().HasKey(ci => new {
                ci.OrderId,
                ci.ProductId
            });
        }

        protected void SetupProvinceModel(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Province>().
                HasRequired(p => p.Country).
                WithMany(c => c.Provinces).
                HasForeignKey(p => p.CountryCode).
                WillCascadeOnDelete(false);

            modelBuilder.Entity<Province>().
                HasKey(p => new { p.ProvinceCode, p.CountryCode });
        }

        protected void SetupAddressAndDerivedModels(DbModelBuilder modelBuilder)
        {
            // Foreign keys:
            // Province: (ProvinceCode, CountryCode)
            // Country: CountryCode

            modelBuilder.Entity<Address>().
                HasRequired(a => a.Province).
                WithMany().
                HasForeignKey(a => new { a.ProvinceCode, a.CountryCode}).
                WillCascadeOnDelete(false);

            modelBuilder.Entity<Address>().
                HasRequired(a => a.Country).
                WithMany().
                HasForeignKey(a => a.CountryCode).
                WillCascadeOnDelete(false);

            /* Member Address Setup */

            // Primary Key:
            // MemberId, Id
            modelBuilder.Entity<MemberAddress>().
                HasKey(ma => new { ma.Id, ma.MemberId });

            // Foreign Keys:
            // Member: MemberId
            modelBuilder.Entity<MemberAddress>().
                HasRequired(ma => ma.Member).
                WithMany(m => m.ShippingAddresses).
                HasForeignKey(ma => ma.MemberId).
                WillCascadeOnDelete(true);

            modelBuilder.Entity<MemberAddress>().
                HasRequired(ma => ma.Member).
                WithMany(m => m.BillingAddresses).
                HasForeignKey(ma => ma.MemberId).
                WillCascadeOnDelete(true);

            /* Location Setup */

            // Foreign Key to Location Type
            modelBuilder.Entity<LocationType>().
                HasMany(lt => lt.Locations).
                WithRequired().
                HasForeignKey(l => l.LocationTypeName).
                WillCascadeOnDelete(false);

            // Table Per Concrete Type:
            // Location
            // Member Address
            modelBuilder.Entity<MemberAddress>().
                Map(t => 
                    t.MapInheritedProperties().
                        ToTable(nameof(MemberAddress)));

            modelBuilder.Entity<Location>().
                Map(t =>
                    t.MapInheritedProperties().
                        ToTable(nameof(Location)));
        }

        protected void SetupPersonAndDerivedModels(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Member>().
                HasMany(m => m.FavoritePlatforms).
                WithMany(p => p.MembersFavoritePlatform).
                Map(
                    mToMconf =>
                    {
                        mToMconf.ToTable("MemberFavoritePlatform");
                    });

            modelBuilder.Entity<Member>().
                HasMany(m => m.FavoriteTags).
                WithMany(t => t.MemberFavoriteCategory).
                Map(
                    mToMconf =>
                    {
                        mToMconf.ToTable("MemberFavoriteTag");
                    });

            modelBuilder.Entity<Member>().
                HasMany(m => m.Wishlist).
                WithMany().
                Map(t => t.MapLeftKey("MemberId").
                        MapRightKey("ProductId").
                        ToTable("MemberWishlistItem"));

            modelBuilder.Entity<Member>().
                HasMany(m => m.RegisteredEvents).
                WithMany(e => e.RegisteredMembers).
                Map(
                    mToMconf =>
                    {
                        mToMconf.ToTable("MemberEventMembership");
                    });

            modelBuilder.Entity<Member>().
                HasMany(m => m.PaymentInformation).
                WithRequired(cc => cc.Member).
                HasForeignKey(cc => cc.MemberId);

            modelBuilder.Entity<Member>().
                HasKey(m => m.PersonId).
                Property(m => m.PersonId).
                HasColumnName("MemberId");

            modelBuilder.Entity<Member>().ToTable("Member");

            // Member => BillingAddress is setup in SetupAddressAndDerivedModels
            // Member => ShippingAddress is setup in SetupAddressAndDerivedModels
            // Member => Cart is setup in SetupCartModel

            modelBuilder.Entity<Employee>().
                HasRequired(emp => emp.StoreLocation).
                WithMany().
                HasForeignKey(emp => emp.StoreLocationId);

            modelBuilder.Entity<Employee>().
                HasRequired(emp => emp.Department).
                WithMany().
                HasForeignKey(emp => emp.DepartmentId);

            modelBuilder.Entity<Employee>().
                Property(emp => emp.EmployeeId).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity).
                HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute("IX_EmployeeId")
                        {
                            IsUnique = true
                        }
                    )
                );
        }

        protected void SetupProductAndDerivedModels(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GameProduct>().
                HasRequired(gp => gp.Platform).
                WithMany(p => p.GameProducts).
                HasForeignKey(gp => gp.PlatformCode);

            modelBuilder.Entity<GameProduct>().
                HasRequired(gp => gp.Game).
                WithMany(g => g.GameSKUs).
                HasForeignKey(gp => gp.GameId);

            // Table per type for Products
            modelBuilder.Entity<GameProduct>().ToTable("GameProduct");
            modelBuilder.Entity<PhysicalGameProduct>().ToTable("PhysicalGameProduct");
            modelBuilder.Entity<DownloadGameProduct>().ToTable("DownloadGameProduct");
        }

        protected void SetupCartModel(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cart>().
                HasRequired(c => c.Member).
                WithOptional(m => m.Cart);

            modelBuilder.Entity<Cart>().
                HasMany(m => m.Items).
                WithRequired().
                HasForeignKey(ci => ci.MemberId);
        }

        protected void SetupCartItemModel(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CartItem>().
                HasRequired(ci => ci.Product).
                WithMany().
                HasForeignKey(ci => ci.ProductId).
                WillCascadeOnDelete(false);

            modelBuilder.Entity<CartItem>().HasKey(ci => new { ci.MemberId, ci.ProductId });
        }

        protected void SetupReviewAndDerivedModels(DbModelBuilder modelBuilder)
        {
            /*modelBuilder.Entity<Review>().
                HasRequired(r => r.Member).
                WithMany().
                HasForeignKey(r => r.MemberId);*/

            // TODO: GameReview -> Game Product or Review -> Product with a collection navigation property on the review

            modelBuilder.Entity<GameReview>().
                HasRequired(gr => gr.GameProduct).
                WithMany().
                HasForeignKey(gr => gr.GameProductId);

            modelBuilder.Entity<GameReview>().
                HasRequired(g => g.Member).
                WithMany().
                HasForeignKey(g => g.MemberId);

            modelBuilder.Entity<GameReview>().
                HasKey(g => new { g.MemberId, g.GameProductId });

            modelBuilder.Entity<GameReview>().
                Map(t => 
                    t.MapInheritedProperties().
                        ToTable(nameof(GameReview)));
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // TODO: Tables per concrete type for:
            // Address: Location & Member Address
            // Person: Employee & Member TODO: Figure out how this will work with Identity

            // TODO: Foreign keys:
            // Address: (ProvinceCode, CountryCode), CountryCode
            //      Location: LocationTypeName
            //      MemberAddress: MemberId
            // GameProduct: PublisherId, DeveloperId, PlatformCode, GameId
            // GameReview: GameProductId
            // ProductLocationInventory: ProductId, LocationId
            // Province: CountryCode
            // WebOrder: (AddressId, MemberId), MemberId, CreditCardPaymentInformation
            // CreditCardPaymentInformation: MemberId

            // TODO: Composite keys:
            // MemberAddress: Id, MemberId
            // Province: ProvinceCode, CountryCode
            // CreditCardPaymentInformation: MemberId, CardNumber

            // TODO: No navigation property:
            // Location: LocationType UNSURE: Can we do updates without the navigation property? Does it even matter as we aren't allowing entry of locations
            // Product: ICollection<Member> UNSURE: We won't event want to navigate from a product to the member's with it on their wish list

            // TODO: Missing navigation properties we will want:
            // Product -> Review and/or GameProduct -> Review

            SetupCompanyModel(modelBuilder);
            SetupGameModel(modelBuilder);
            SetupProvinceModel(modelBuilder);
            SetupAddressAndDerivedModels(modelBuilder);
            SetupPersonAndDerivedModels(modelBuilder);
            SetupCartModel(modelBuilder);
            SetupCartItemModel(modelBuilder);
            SetupWebOrderModel(modelBuilder);
            SetupOrderItemModel(modelBuilder);
            SetupProductAndDerivedModels(modelBuilder);
            SetupReviewAndDerivedModels(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }
    }
}
