namespace Veil.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class Setup_Event_Id_AddBackCascadeDelete : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.MemberEventMembership", "Event_Id", "dbo.Event");
            DropPrimaryKey("dbo.Event");
            AlterColumn("dbo.Event", "Id", c => c.Guid(nullable: false, identity: true));
            AddPrimaryKey("dbo.Event", "Id");
            AddForeignKey("dbo.MemberEventMembership", "Event_Id", "dbo.Event", "Id");

            DropForeignKey("dbo.MemberEventMembership", "Event_Id", "dbo.Event");
            DropForeignKey("dbo.MemberEventMembership", "Member_UserId", "dbo.Member");
            AddForeignKey("dbo.MemberEventMembership", "Event_Id", "dbo.Event", "Id", cascadeDelete: true);
            AddForeignKey("dbo.MemberEventMembership", "Member_UserId", "dbo.Member", "UserId", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.MemberEventMembership", "Event_Id", "dbo.Event");
            DropPrimaryKey("dbo.Event");
            AlterColumn("dbo.Event", "Id", c => c.Guid(nullable: false));
            AddPrimaryKey("dbo.Event", "Id");
            AddForeignKey("dbo.MemberEventMembership", "Event_Id", "dbo.Event", "Id");

            DropForeignKey("dbo.MemberEventMembership", "Event_Id", "dbo.Event");
            DropForeignKey("dbo.MemberEventMembership", "Member_UserId", "dbo.Member");
            AddForeignKey("dbo.MemberEventMembership", "Member_UserId", "dbo.Member", "UserId");
            AddForeignKey("dbo.MemberEventMembership", "Event_Id", "dbo.Event", "Id");
        }
    }
}
