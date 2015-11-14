namespace Veil.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class WebOrder_CreditCard_Changes : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.WebOrder", "MemberCreditCardId", "dbo.MemberCreditCard");
            DropIndex("dbo.WebOrder", new[] { "MemberCreditCardId" });
            AddColumn("dbo.WebOrder", "CreditCardLast4Digits", c => c.String(nullable: false, maxLength: 4));
            DropColumn("dbo.WebOrder", "MemberCreditCardId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.WebOrder", "MemberCreditCardId", c => c.Guid(nullable: false));
            DropColumn("dbo.WebOrder", "CreditCardLast4Digits");
            CreateIndex("dbo.WebOrder", "MemberCreditCardId");
            AddForeignKey("dbo.WebOrder", "MemberCreditCardId", "dbo.MemberCreditCard", "Id");
        }
    }
}
