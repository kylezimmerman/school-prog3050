namespace Veil.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Updated_OrderItem_PK_ToMatchCartItem : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.OrderItem");
            AddPrimaryKey("dbo.OrderItem", new[] { "OrderId", "ProductId", "IsNew" });
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.OrderItem");
            AddPrimaryKey("dbo.OrderItem", new[] { "OrderId", "ProductId" });
        }
    }
}
