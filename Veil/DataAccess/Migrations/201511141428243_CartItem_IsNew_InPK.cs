namespace Veil.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class CartItem_IsNew_InPK : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.CartItem");
            AddPrimaryKey("dbo.CartItem", new[] { "MemberId", "ProductId", "IsNew" });
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.CartItem");
            AddPrimaryKey("dbo.CartItem", new[] { "MemberId", "ProductId" });
        }
    }
}
