namespace Veil.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class WebOrder_Address_Changes : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.WebOrder", "ShippingAddressId", "dbo.MemberAddress");
            DropIndex("dbo.WebOrder", new[] { "ShippingAddressId" });
            AddColumn("dbo.WebOrder", "StreetAddress", c => c.String(nullable: false, maxLength: 255));
            AddColumn("dbo.WebOrder", "POBoxNumber", c => c.String(maxLength: 16));
            AddColumn("dbo.WebOrder", "City", c => c.String(nullable: false, maxLength: 255));
            AddColumn("dbo.WebOrder", "PostalCode", c => c.String(nullable: false, maxLength: 16));
            AddColumn("dbo.WebOrder", "ProvinceCode", c => c.String(nullable: false, maxLength: 2));
            AddColumn("dbo.WebOrder", "CountryCode", c => c.String(nullable: false, maxLength: 2));
            CreateIndex("dbo.WebOrder", new[] { "ProvinceCode", "CountryCode" });
            CreateIndex("dbo.WebOrder", "CountryCode");
            AddForeignKey("dbo.WebOrder", "CountryCode", "dbo.Country", "CountryCode");
            AddForeignKey("dbo.WebOrder", new[] { "ProvinceCode", "CountryCode" }, "dbo.Province", new[] { "ProvinceCode", "CountryCode" });
            DropColumn("dbo.WebOrder", "ShippingAddressId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.WebOrder", "ShippingAddressId", c => c.Guid(nullable: false));
            DropForeignKey("dbo.WebOrder", new[] { "ProvinceCode", "CountryCode" }, "dbo.Province");
            DropForeignKey("dbo.WebOrder", "CountryCode", "dbo.Country");
            DropIndex("dbo.WebOrder", new[] { "CountryCode" });
            DropIndex("dbo.WebOrder", new[] { "ProvinceCode", "CountryCode" });
            DropColumn("dbo.WebOrder", "CountryCode");
            DropColumn("dbo.WebOrder", "ProvinceCode");
            DropColumn("dbo.WebOrder", "PostalCode");
            DropColumn("dbo.WebOrder", "City");
            DropColumn("dbo.WebOrder", "POBoxNumber");
            DropColumn("dbo.WebOrder", "StreetAddress");
            CreateIndex("dbo.WebOrder", "ShippingAddressId");
            AddForeignKey("dbo.WebOrder", "ShippingAddressId", "dbo.MemberAddress", "Id");
        }
    }
}
