namespace Veil.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addesrbimageurl : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ESRBRating", "ImageURL", c => c.String(maxLength: 2048));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ESRBRating", "ImageURL");
        }
    }
}
