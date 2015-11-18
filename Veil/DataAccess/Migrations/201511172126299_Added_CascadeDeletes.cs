namespace Veil.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Added_CascadeDeletes : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.MemberCreditCard", "MemberId", "dbo.Member");
            DropForeignKey("dbo.UserClaim", "UserId", "dbo.User");
            DropForeignKey("dbo.UserLogin", "UserId", "dbo.User");
            DropForeignKey("dbo.GameReview", "MemberId", "dbo.Member");

            // Not specified by EF
            DropForeignKey("dbo.GameReview", "ProductReviewedId", "dbo.GameProduct");
            DropForeignKey("dbo.GameESRBContentDescriptor", "Game_Id", "dbo.Game");
            DropForeignKey("dbo.GameCategory", "Game_Id", "dbo.Game");

            AddForeignKey("dbo.MemberCreditCard", "MemberId", "dbo.Member", "UserId", cascadeDelete: true);
            AddForeignKey("dbo.UserClaim", "UserId", "dbo.User", "Id", cascadeDelete: true);
            AddForeignKey("dbo.UserLogin", "UserId", "dbo.User", "Id", cascadeDelete: true);
            AddForeignKey("dbo.GameReview", "MemberId", "dbo.Member", "UserId", cascadeDelete: true);

            // Not specified by EF
            AddForeignKey("dbo.GameReview", "ProductReviewedId", "dbo.GameProduct", "Id", cascadeDelete: true);
            AddForeignKey("dbo.GameCategory", "Game_Id", "dbo.Game", "Id", cascadeDelete: true);
            AddForeignKey("dbo.GameESRBContentDescriptor", "Game_Id", "dbo.Game", "Id", cascadeDelete: true);
        }

        public override void Down()
        {
            DropForeignKey("dbo.GameReview", "MemberId", "dbo.Member");
            DropForeignKey("dbo.UserLogin", "UserId", "dbo.User");
            DropForeignKey("dbo.UserClaim", "UserId", "dbo.User");
            DropForeignKey("dbo.MemberCreditCard", "MemberId", "dbo.Member");

            // Not specified by EF
            DropForeignKey("dbo.GameReview", "ProductReviewedId", "dbo.GameProduct");
            DropForeignKey("dbo.GameCategory", "Game_Id", "dbo.Game");
            DropForeignKey("dbo.GameESRBContentDescriptor", "Game_Id", "dbo.Game");

            AddForeignKey("dbo.GameReview", "MemberId", "dbo.Member", "UserId");
            AddForeignKey("dbo.UserLogin", "UserId", "dbo.User", "Id");
            AddForeignKey("dbo.UserClaim", "UserId", "dbo.User", "Id");
            AddForeignKey("dbo.MemberCreditCard", "MemberId", "dbo.Member", "UserId");

            // Not specified by EF
            AddForeignKey("dbo.GameReview", "ProductReviewedId", "dbo.GameProduct", "Id");
            AddForeignKey("dbo.GameCategory", "Game_Id", "dbo.Game", "Id");
            AddForeignKey("dbo.GameESRBContentDescriptor", "Game_Id", "dbo.Game", "Id");
        }
    }
}
