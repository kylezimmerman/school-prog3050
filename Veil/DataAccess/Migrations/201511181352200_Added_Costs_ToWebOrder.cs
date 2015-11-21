namespace Veil.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Added_Costs_ToWebOrder : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.WebOrder", "TaxAmount", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.WebOrder", "ShippingCost", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.WebOrder", "OrderSubtotal", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.WebOrder", "OrderSubtotal");
            DropColumn("dbo.WebOrder", "ShippingCost");
            DropColumn("dbo.WebOrder", "TaxAmount");
        }
    }
}
