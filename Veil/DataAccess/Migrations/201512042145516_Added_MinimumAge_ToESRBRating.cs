namespace Veil.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Added_MinimumAge_ToESRBRating : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ESRBRating", "MinimumAge", c => c.Byte(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ESRBRating", "MinimumAge");
        }
    }
}
