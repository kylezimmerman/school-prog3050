namespace Veil.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Reduced_User_PhoneNumber_Size : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.User", "PhoneNumber", c => c.String(maxLength: 32));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.User", "PhoneNumber", c => c.String());
        }
    }
}
