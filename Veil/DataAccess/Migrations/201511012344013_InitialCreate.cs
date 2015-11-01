namespace Veil.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    /// <summary>
    /// Migration which sets up the database to it base version. 
    /// This is equivalent to how EF sets it up without migrations enable
    /// </summary>
    /// 
    /// NOTE: DO NOT MANUALLY MODIFY THIS FILE AS ALL CHANGES WILL BE LOST UPON COLLAPSING MIGRATIONS DOWN INTO ONE
    /// 
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CartItem",
                c => new
                    {
                        MemberId = c.Guid(nullable: false),
                        ProductId = c.Guid(nullable: false),
                        IsNew = c.Boolean(nullable: false),
                        Quantity = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.MemberId, t.ProductId })
                .ForeignKey("dbo.Product", t => t.ProductId)
                .ForeignKey("dbo.Member", t => t.MemberId)
                .Index(t => t.MemberId)
                .Index(t => t.ProductId);
            
            CreateTable(
                "dbo.Product",
                c => new
                    {
                        Id = c.Guid(nullable: false, identity: true),
                        ProductAvailabilityStatus = c.Int(nullable: false),
                        ReleaseDate = c.DateTime(nullable: false),
                        NewWebPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                        UsedWebPrice = c.Decimal(precision: 18, scale: 2),
                        BoxArtImageURL = c.String(maxLength: 2048),
                        SKUDescription = c.String(maxLength: 1024),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ProductLocationInventory",
                c => new
                    {
                        LocationId = c.Guid(nullable: false),
                        ProductId = c.Guid(nullable: false),
                        NewOnHand = c.Int(nullable: false),
                        NewOnOrder = c.Int(nullable: false),
                        UsedOnHand = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.LocationId, t.ProductId })
                .ForeignKey("dbo.Location", t => t.LocationId)
                .ForeignKey("dbo.Product", t => t.ProductId)
                .Index(t => t.LocationId)
                .Index(t => t.ProductId);
            
            CreateTable(
                "dbo.Location",
                c => new
                    {
                        Id = c.Guid(nullable: false, identity: true),
                        LocationNumber = c.Int(nullable: false),
                        LocationTypeName = c.String(nullable: false, maxLength: 64),
                        SiteName = c.String(nullable: false, maxLength: 128),
                        PhoneNumber = c.String(nullable: false, maxLength: 32),
                        FaxNumber = c.String(maxLength: 32),
                        TollFreeNumber = c.String(maxLength: 32),
                        StreetAddress = c.String(nullable: false, maxLength: 255),
                        POBoxNumber = c.String(maxLength: 16),
                        City = c.String(nullable: false, maxLength: 255),
                        PostalCode = c.String(nullable: false, maxLength: 16),
                        ProvinceCode = c.String(nullable: false, maxLength: 2),
                        CountryCode = c.String(nullable: false, maxLength: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Country", t => t.CountryCode)
                .ForeignKey("dbo.Province", t => new { t.ProvinceCode, t.CountryCode })
                .ForeignKey("dbo.LocationType", t => t.LocationTypeName)
                .Index(t => t.LocationTypeName)
                .Index(t => new { t.ProvinceCode, t.CountryCode })
                .Index(t => t.CountryCode);
            
            CreateTable(
                "dbo.Country",
                c => new
                    {
                        CountryCode = c.String(nullable: false, maxLength: 2),
                        CountryName = c.String(nullable: false, maxLength: 255),
                        FederalTaxRate = c.Decimal(nullable: false, precision: 18, scale: 2),
                        FederalTaxAcronym = c.String(maxLength: 16),
                    })
                .PrimaryKey(t => t.CountryCode);
            
            CreateTable(
                "dbo.Province",
                c => new
                    {
                        ProvinceCode = c.String(nullable: false, maxLength: 2),
                        CountryCode = c.String(nullable: false, maxLength: 2),
                        Name = c.String(nullable: false, maxLength: 255),
                        ProvincialTaxRate = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ProvincialTaxAcronym = c.String(maxLength: 16),
                    })
                .PrimaryKey(t => new { t.ProvinceCode, t.CountryCode })
                .ForeignKey("dbo.Country", t => t.CountryCode)
                .Index(t => t.CountryCode);
            
            CreateTable(
                "dbo.Company",
                c => new
                    {
                        Id = c.Guid(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 512),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Game",
                c => new
                    {
                        Id = c.Guid(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 512),
                        ESRBRatingId = c.String(nullable: false, maxLength: 8),
                        MinimumPlayerCount = c.Int(nullable: false),
                        MaximumPlayerCount = c.Int(nullable: false),
                        TrailerURL = c.String(maxLength: 2048),
                        ShortDescription = c.String(nullable: false, maxLength: 140),
                        LongDescription = c.String(maxLength: 2048),
                        PrimaryImageURL = c.String(maxLength: 2048),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ESRBRating", t => t.ESRBRatingId)
                .Index(t => t.ESRBRatingId);
            
            CreateTable(
                "dbo.ESRBContentDescriptor",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DescriptorName = c.String(nullable: false, maxLength: 64),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ESRBRating",
                c => new
                    {
                        RatingId = c.String(nullable: false, maxLength: 8),
                        Description = c.String(nullable: false, maxLength: 64),
                        ImageURL = c.String(maxLength: 2048),
                    })
                .PrimaryKey(t => t.RatingId);
            
            CreateTable(
                "dbo.Tag",
                c => new
                    {
                        Name = c.String(nullable: false, maxLength: 64),
                    })
                .PrimaryKey(t => t.Name);
            
            CreateTable(
                "dbo.Member",
                c => new
                    {
                        UserId = c.Guid(nullable: false),
                        StripeCustomerId = c.String(nullable: false, maxLength: 255),
                        ReceivePromotionalEmails = c.Boolean(nullable: false),
                        WishListVisibility = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.UserId)
                .ForeignKey("dbo.User", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.MemberCreditCard",
                c => new
                    {
                        Id = c.Guid(nullable: false, identity: true),
                        MemberId = c.Guid(nullable: false),
                        StripeCardId = c.String(nullable: false, maxLength: 255),
                        Last4Digits = c.String(nullable: false, maxLength: 4),
                        ExpiryMonth = c.Int(nullable: false),
                        ExpiryYear = c.Int(nullable: false),
                        CardholderName = c.String(nullable: false, maxLength: 255),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Member", t => t.MemberId)
                .Index(t => t.MemberId);
            
            CreateTable(
                "dbo.Platform",
                c => new
                    {
                        PlatformCode = c.String(nullable: false, maxLength: 5),
                        PlatformName = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.PlatformCode);
            
            CreateTable(
                "dbo.Friendship",
                c => new
                    {
                        ReceiverId = c.Guid(nullable: false),
                        RequesterId = c.Guid(nullable: false),
                        RequestStatus = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.ReceiverId, t.RequesterId })
                .ForeignKey("dbo.Member", t => t.ReceiverId)
                .ForeignKey("dbo.Member", t => t.RequesterId)
                .Index(t => t.ReceiverId)
                .Index(t => t.RequesterId);
            
            CreateTable(
                "dbo.Event",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Name = c.String(nullable: false, maxLength: 255),
                        Description = c.String(maxLength: 2048),
                        Date = c.DateTime(nullable: false),
                        Duration = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.MemberAddress",
                c => new
                    {
                        Id = c.Guid(nullable: false, identity: true),
                        MemberId = c.Guid(nullable: false),
                        StreetAddress = c.String(nullable: false, maxLength: 255),
                        POBoxNumber = c.String(maxLength: 16),
                        City = c.String(nullable: false, maxLength: 255),
                        PostalCode = c.String(nullable: false, maxLength: 16),
                        ProvinceCode = c.String(nullable: false, maxLength: 2),
                        CountryCode = c.String(nullable: false, maxLength: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Country", t => t.CountryCode)
                .ForeignKey("dbo.Member", t => t.MemberId, cascadeDelete: true)
                .ForeignKey("dbo.Province", t => new { t.ProvinceCode, t.CountryCode })
                .Index(t => t.MemberId)
                .Index(t => new { t.ProvinceCode, t.CountryCode })
                .Index(t => t.CountryCode);
            
            CreateTable(
                "dbo.User",
                c => new
                    {
                        Id = c.Guid(nullable: false, identity: true),
                        Email = c.String(nullable: false, maxLength: 256),
                        UserName = c.String(nullable: false, maxLength: 256),
                        FirstName = c.String(nullable: false, maxLength: 64),
                        LastName = c.String(nullable: false, maxLength: 64),
                        EmailConfirmed = c.Boolean(nullable: false),
                        PasswordHash = c.String(),
                        SecurityStamp = c.String(),
                        PhoneNumber = c.String(),
                        PhoneNumberConfirmed = c.Boolean(nullable: false),
                        TwoFactorEnabled = c.Boolean(nullable: false),
                        LockoutEndDateUtc = c.DateTime(),
                        LockoutEnabled = c.Boolean(nullable: false),
                        AccessFailedCount = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName, unique: true, name: "UserNameIndex");
            
            CreateTable(
                "dbo.UserClaim",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.Guid(nullable: false),
                        ClaimType = c.String(),
                        ClaimValue = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.User", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Employee",
                c => new
                    {
                        EmployeeUserId = c.Guid(nullable: false),
                        EmployeeId = c.Int(nullable: false, identity: true),
                        StoreLocationId = c.Guid(nullable: false),
                        DepartmentId = c.Int(nullable: false),
                        HireDate = c.DateTime(nullable: false),
                        TerminationDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.EmployeeUserId)
                .ForeignKey("dbo.Department", t => t.DepartmentId)
                .ForeignKey("dbo.Location", t => t.StoreLocationId)
                .ForeignKey("dbo.User", t => t.EmployeeUserId)
                .Index(t => t.EmployeeUserId)
                .Index(t => t.EmployeeId, unique: true, name: "Employee_IX_EmployeeId_UQ")
                .Index(t => t.StoreLocationId)
                .Index(t => t.DepartmentId);
            
            CreateTable(
                "dbo.Department",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.UserLogin",
                c => new
                    {
                        LoginProvider = c.String(nullable: false, maxLength: 128),
                        ProviderKey = c.String(nullable: false, maxLength: 128),
                        UserId = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.User", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.UserRole",
                c => new
                    {
                        UserId = c.Guid(nullable: false),
                        RoleId = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.User", t => t.UserId, cascadeDelete: true)
                .ForeignKey("dbo.Role", t => t.RoleId)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);
            
            CreateTable(
                "dbo.WebOrder",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        MemberId = c.Guid(nullable: false),
                        MemberCreditCardId = c.Guid(nullable: false),
                        ShippingAddressId = c.Guid(nullable: false),
                        StripeChargeId = c.String(nullable: false, maxLength: 255),
                        OrderDate = c.DateTime(nullable: false),
                        OrderStatus = c.Int(nullable: false),
                        ProcessedDate = c.DateTime(),
                        ReasonForCancellationMessage = c.String(maxLength: 512),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Member", t => t.MemberId)
                .ForeignKey("dbo.MemberCreditCard", t => t.MemberCreditCardId)
                .ForeignKey("dbo.MemberAddress", t => t.ShippingAddressId)
                .Index(t => t.MemberId)
                .Index(t => t.MemberCreditCardId)
                .Index(t => t.ShippingAddressId);
            
            CreateTable(
                "dbo.OrderItem",
                c => new
                    {
                        OrderId = c.Long(nullable: false),
                        ProductId = c.Guid(nullable: false),
                        IsNew = c.Boolean(nullable: false),
                        Quantity = c.Int(nullable: false),
                        ListPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.OrderId, t.ProductId })
                .ForeignKey("dbo.Product", t => t.ProductId)
                .ForeignKey("dbo.WebOrder", t => t.OrderId)
                .Index(t => t.OrderId)
                .Index(t => t.ProductId);
            
            CreateTable(
                "dbo.GameReview",
                c => new
                    {
                        MemberId = c.Guid(nullable: false),
                        ProductReviewedId = c.Guid(nullable: false),
                        Rating = c.Int(nullable: false),
                        ReviewText = c.String(maxLength: 4000),
                        ReviewStatus = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.MemberId, t.ProductReviewedId })
                .ForeignKey("dbo.Member", t => t.MemberId)
                .ForeignKey("dbo.GameProduct", t => t.ProductReviewedId)
                .Index(t => t.MemberId)
                .Index(t => t.ProductReviewedId);
            
            CreateTable(
                "dbo.LocationType",
                c => new
                    {
                        LocationTypeName = c.String(nullable: false, maxLength: 64),
                    })
                .PrimaryKey(t => t.LocationTypeName);
            
            CreateTable(
                "dbo.Role",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Name = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "RoleNameIndex");
            
            CreateTable(
                "dbo.GameESRBContentDescriptor",
                c => new
                    {
                        Game_Id = c.Guid(nullable: false),
                        ESRBContentDescriptor_Id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Game_Id, t.ESRBContentDescriptor_Id })
                .ForeignKey("dbo.Game", t => t.Game_Id, cascadeDelete: true)
                .ForeignKey("dbo.ESRBContentDescriptor", t => t.ESRBContentDescriptor_Id, cascadeDelete: true)
                .Index(t => t.Game_Id)
                .Index(t => t.ESRBContentDescriptor_Id);
            
            CreateTable(
                "dbo.MemberFavoritePlatform",
                c => new
                    {
                        Member_UserId = c.Guid(nullable: false),
                        Platform_PlatformCode = c.String(nullable: false, maxLength: 5),
                    })
                .PrimaryKey(t => new { t.Member_UserId, t.Platform_PlatformCode })
                .ForeignKey("dbo.Member", t => t.Member_UserId, cascadeDelete: true)
                .ForeignKey("dbo.Platform", t => t.Platform_PlatformCode, cascadeDelete: true)
                .Index(t => t.Member_UserId)
                .Index(t => t.Platform_PlatformCode);
            
            CreateTable(
                "dbo.MemberFavoriteTag",
                c => new
                    {
                        Member_UserId = c.Guid(nullable: false),
                        Tag_Name = c.String(nullable: false, maxLength: 64),
                    })
                .PrimaryKey(t => new { t.Member_UserId, t.Tag_Name })
                .ForeignKey("dbo.Member", t => t.Member_UserId, cascadeDelete: true)
                .ForeignKey("dbo.Tag", t => t.Tag_Name, cascadeDelete: true)
                .Index(t => t.Member_UserId)
                .Index(t => t.Tag_Name);
            
            CreateTable(
                "dbo.MemberEventMembership",
                c => new
                    {
                        Member_UserId = c.Guid(nullable: false),
                        Event_Id = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => new { t.Member_UserId, t.Event_Id })
                .ForeignKey("dbo.Member", t => t.Member_UserId, cascadeDelete: true)
                .ForeignKey("dbo.Event", t => t.Event_Id, cascadeDelete: true)
                .Index(t => t.Member_UserId)
                .Index(t => t.Event_Id);
            
            CreateTable(
                "dbo.MemberWishlistItem",
                c => new
                    {
                        MemberId = c.Guid(nullable: false),
                        ProductId = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => new { t.MemberId, t.ProductId })
                .ForeignKey("dbo.Member", t => t.MemberId, cascadeDelete: true)
                .ForeignKey("dbo.Product", t => t.ProductId, cascadeDelete: true)
                .Index(t => t.MemberId)
                .Index(t => t.ProductId);
            
            CreateTable(
                "dbo.GameCategory",
                c => new
                    {
                        Game_Id = c.Guid(nullable: false),
                        Tag_Name = c.String(nullable: false, maxLength: 64),
                    })
                .PrimaryKey(t => new { t.Game_Id, t.Tag_Name })
                .ForeignKey("dbo.Game", t => t.Game_Id, cascadeDelete: true)
                .ForeignKey("dbo.Tag", t => t.Tag_Name, cascadeDelete: true)
                .Index(t => t.Game_Id)
                .Index(t => t.Tag_Name);
            
            CreateTable(
                "dbo.GameProduct",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        PublisherId = c.Guid(nullable: false),
                        DeveloperId = c.Guid(nullable: false),
                        PlatformCode = c.String(nullable: false, maxLength: 5),
                        GameId = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Product", t => t.Id)
                .ForeignKey("dbo.Company", t => t.PublisherId)
                .ForeignKey("dbo.Company", t => t.DeveloperId)
                .ForeignKey("dbo.Platform", t => t.PlatformCode)
                .ForeignKey("dbo.Game", t => t.GameId)
                .Index(t => t.Id)
                .Index(t => t.PublisherId)
                .Index(t => t.DeveloperId)
                .Index(t => t.PlatformCode)
                .Index(t => t.GameId);
            
            CreateTable(
                "dbo.DownloadGameProduct",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        DownloadLink = c.String(nullable: false, maxLength: 2048),
                        ApproximateSizeInMB = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.GameProduct", t => t.Id)
                .Index(t => t.Id);
            
            CreateTable(
                "dbo.PhysicalGameProduct",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        SKUNameSuffix = c.String(maxLength: 255),
                        InternalNewSKU = c.String(maxLength: 128),
                        InteralUsedSKU = c.String(maxLength: 128),
                        WillBuyBackUsedCopy = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.GameProduct", t => t.Id)
                .Index(t => t.Id)
                .Index(t => t.InternalNewSKU, unique: true, name: "PhysicalGameProduct_IX_InternalNewSKU_UQ")
                .Index(t => t.InteralUsedSKU, unique: true, name: "PhysicalGameProduct_IX_InternalUsedSKU_UQ");
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PhysicalGameProduct", "Id", "dbo.GameProduct");
            DropForeignKey("dbo.DownloadGameProduct", "Id", "dbo.GameProduct");
            DropForeignKey("dbo.GameProduct", "GameId", "dbo.Game");
            DropForeignKey("dbo.GameProduct", "PlatformCode", "dbo.Platform");
            DropForeignKey("dbo.GameProduct", "DeveloperId", "dbo.Company");
            DropForeignKey("dbo.GameProduct", "PublisherId", "dbo.Company");
            DropForeignKey("dbo.GameProduct", "Id", "dbo.Product");
            DropForeignKey("dbo.UserRole", "RoleId", "dbo.Role");
            DropForeignKey("dbo.Location", "LocationTypeName", "dbo.LocationType");
            DropForeignKey("dbo.CartItem", "MemberId", "dbo.Member");
            DropForeignKey("dbo.CartItem", "ProductId", "dbo.Product");
            DropForeignKey("dbo.GameReview", "ProductReviewedId", "dbo.GameProduct");
            DropForeignKey("dbo.GameReview", "MemberId", "dbo.Member");
            DropForeignKey("dbo.GameCategory", "Tag_Name", "dbo.Tag");
            DropForeignKey("dbo.GameCategory", "Game_Id", "dbo.Game");
            DropForeignKey("dbo.MemberWishlistItem", "ProductId", "dbo.Product");
            DropForeignKey("dbo.MemberWishlistItem", "MemberId", "dbo.Member");
            DropForeignKey("dbo.WebOrder", "ShippingAddressId", "dbo.MemberAddress");
            DropForeignKey("dbo.OrderItem", "OrderId", "dbo.WebOrder");
            DropForeignKey("dbo.OrderItem", "ProductId", "dbo.Product");
            DropForeignKey("dbo.WebOrder", "MemberCreditCardId", "dbo.MemberCreditCard");
            DropForeignKey("dbo.WebOrder", "MemberId", "dbo.Member");
            DropForeignKey("dbo.Member", "UserId", "dbo.User");
            DropForeignKey("dbo.UserRole", "UserId", "dbo.User");
            DropForeignKey("dbo.UserLogin", "UserId", "dbo.User");
            DropForeignKey("dbo.Employee", "EmployeeUserId", "dbo.User");
            DropForeignKey("dbo.Employee", "StoreLocationId", "dbo.Location");
            DropForeignKey("dbo.Employee", "DepartmentId", "dbo.Department");
            DropForeignKey("dbo.UserClaim", "UserId", "dbo.User");
            DropForeignKey("dbo.MemberAddress", new[] { "ProvinceCode", "CountryCode" }, "dbo.Province");
            DropForeignKey("dbo.MemberAddress", "MemberId", "dbo.Member");
            DropForeignKey("dbo.MemberAddress", "CountryCode", "dbo.Country");
            DropForeignKey("dbo.MemberEventMembership", "Event_Id", "dbo.Event");
            DropForeignKey("dbo.MemberEventMembership", "Member_UserId", "dbo.Member");
            DropForeignKey("dbo.Friendship", "RequesterId", "dbo.Member");
            DropForeignKey("dbo.Friendship", "ReceiverId", "dbo.Member");
            DropForeignKey("dbo.MemberFavoriteTag", "Tag_Name", "dbo.Tag");
            DropForeignKey("dbo.MemberFavoriteTag", "Member_UserId", "dbo.Member");
            DropForeignKey("dbo.MemberFavoritePlatform", "Platform_PlatformCode", "dbo.Platform");
            DropForeignKey("dbo.MemberFavoritePlatform", "Member_UserId", "dbo.Member");
            DropForeignKey("dbo.MemberCreditCard", "MemberId", "dbo.Member");
            DropForeignKey("dbo.Game", "ESRBRatingId", "dbo.ESRBRating");
            DropForeignKey("dbo.GameESRBContentDescriptor", "ESRBContentDescriptor_Id", "dbo.ESRBContentDescriptor");
            DropForeignKey("dbo.GameESRBContentDescriptor", "Game_Id", "dbo.Game");
            DropForeignKey("dbo.ProductLocationInventory", "ProductId", "dbo.Product");
            DropForeignKey("dbo.ProductLocationInventory", "LocationId", "dbo.Location");
            DropForeignKey("dbo.Location", new[] { "ProvinceCode", "CountryCode" }, "dbo.Province");
            DropForeignKey("dbo.Location", "CountryCode", "dbo.Country");
            DropForeignKey("dbo.Province", "CountryCode", "dbo.Country");
            DropIndex("dbo.PhysicalGameProduct", "PhysicalGameProduct_IX_InternalUsedSKU_UQ");
            DropIndex("dbo.PhysicalGameProduct", "PhysicalGameProduct_IX_InternalNewSKU_UQ");
            DropIndex("dbo.PhysicalGameProduct", new[] { "Id" });
            DropIndex("dbo.DownloadGameProduct", new[] { "Id" });
            DropIndex("dbo.GameProduct", new[] { "GameId" });
            DropIndex("dbo.GameProduct", new[] { "PlatformCode" });
            DropIndex("dbo.GameProduct", new[] { "DeveloperId" });
            DropIndex("dbo.GameProduct", new[] { "PublisherId" });
            DropIndex("dbo.GameProduct", new[] { "Id" });
            DropIndex("dbo.GameCategory", new[] { "Tag_Name" });
            DropIndex("dbo.GameCategory", new[] { "Game_Id" });
            DropIndex("dbo.MemberWishlistItem", new[] { "ProductId" });
            DropIndex("dbo.MemberWishlistItem", new[] { "MemberId" });
            DropIndex("dbo.MemberEventMembership", new[] { "Event_Id" });
            DropIndex("dbo.MemberEventMembership", new[] { "Member_UserId" });
            DropIndex("dbo.MemberFavoriteTag", new[] { "Tag_Name" });
            DropIndex("dbo.MemberFavoriteTag", new[] { "Member_UserId" });
            DropIndex("dbo.MemberFavoritePlatform", new[] { "Platform_PlatformCode" });
            DropIndex("dbo.MemberFavoritePlatform", new[] { "Member_UserId" });
            DropIndex("dbo.GameESRBContentDescriptor", new[] { "ESRBContentDescriptor_Id" });
            DropIndex("dbo.GameESRBContentDescriptor", new[] { "Game_Id" });
            DropIndex("dbo.Role", "RoleNameIndex");
            DropIndex("dbo.GameReview", new[] { "ProductReviewedId" });
            DropIndex("dbo.GameReview", new[] { "MemberId" });
            DropIndex("dbo.OrderItem", new[] { "ProductId" });
            DropIndex("dbo.OrderItem", new[] { "OrderId" });
            DropIndex("dbo.WebOrder", new[] { "ShippingAddressId" });
            DropIndex("dbo.WebOrder", new[] { "MemberCreditCardId" });
            DropIndex("dbo.WebOrder", new[] { "MemberId" });
            DropIndex("dbo.UserRole", new[] { "RoleId" });
            DropIndex("dbo.UserRole", new[] { "UserId" });
            DropIndex("dbo.UserLogin", new[] { "UserId" });
            DropIndex("dbo.Employee", new[] { "DepartmentId" });
            DropIndex("dbo.Employee", new[] { "StoreLocationId" });
            DropIndex("dbo.Employee", "Employee_IX_EmployeeId_UQ");
            DropIndex("dbo.Employee", new[] { "EmployeeUserId" });
            DropIndex("dbo.UserClaim", new[] { "UserId" });
            DropIndex("dbo.User", "UserNameIndex");
            DropIndex("dbo.MemberAddress", new[] { "CountryCode" });
            DropIndex("dbo.MemberAddress", new[] { "ProvinceCode", "CountryCode" });
            DropIndex("dbo.MemberAddress", new[] { "MemberId" });
            DropIndex("dbo.Friendship", new[] { "RequesterId" });
            DropIndex("dbo.Friendship", new[] { "ReceiverId" });
            DropIndex("dbo.MemberCreditCard", new[] { "MemberId" });
            DropIndex("dbo.Member", new[] { "UserId" });
            DropIndex("dbo.Game", new[] { "ESRBRatingId" });
            DropIndex("dbo.Province", new[] { "CountryCode" });
            DropIndex("dbo.Location", new[] { "CountryCode" });
            DropIndex("dbo.Location", new[] { "ProvinceCode", "CountryCode" });
            DropIndex("dbo.Location", new[] { "LocationTypeName" });
            DropIndex("dbo.ProductLocationInventory", new[] { "ProductId" });
            DropIndex("dbo.ProductLocationInventory", new[] { "LocationId" });
            DropIndex("dbo.CartItem", new[] { "ProductId" });
            DropIndex("dbo.CartItem", new[] { "MemberId" });
            DropTable("dbo.PhysicalGameProduct");
            DropTable("dbo.DownloadGameProduct");
            DropTable("dbo.GameProduct");
            DropTable("dbo.GameCategory");
            DropTable("dbo.MemberWishlistItem");
            DropTable("dbo.MemberEventMembership");
            DropTable("dbo.MemberFavoriteTag");
            DropTable("dbo.MemberFavoritePlatform");
            DropTable("dbo.GameESRBContentDescriptor");
            DropTable("dbo.Role");
            DropTable("dbo.LocationType");
            DropTable("dbo.GameReview");
            DropTable("dbo.OrderItem");
            DropTable("dbo.WebOrder");
            DropTable("dbo.UserRole");
            DropTable("dbo.UserLogin");
            DropTable("dbo.Department");
            DropTable("dbo.Employee");
            DropTable("dbo.UserClaim");
            DropTable("dbo.User");
            DropTable("dbo.MemberAddress");
            DropTable("dbo.Event");
            DropTable("dbo.Friendship");
            DropTable("dbo.Platform");
            DropTable("dbo.MemberCreditCard");
            DropTable("dbo.Member");
            DropTable("dbo.Tag");
            DropTable("dbo.ESRBRating");
            DropTable("dbo.ESRBContentDescriptor");
            DropTable("dbo.Game");
            DropTable("dbo.Company");
            DropTable("dbo.Province");
            DropTable("dbo.Country");
            DropTable("dbo.Location");
            DropTable("dbo.ProductLocationInventory");
            DropTable("dbo.Product");
            DropTable("dbo.CartItem");
        }
    }
}
