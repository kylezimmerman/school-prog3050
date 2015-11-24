namespace Veil.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedNewEmailToUser : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.User", "NewEmail", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.User", "NewEmail");
        }
    }
}
