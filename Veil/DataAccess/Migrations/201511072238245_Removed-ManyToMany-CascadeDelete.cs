namespace Veil.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class RemovedManyToManyCascadeDelete : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.GameESRBContentDescriptor", "Game_Id", "dbo.Game");
            DropForeignKey("dbo.GameESRBContentDescriptor", "ESRBContentDescriptor_Id", "dbo.ESRBContentDescriptor");
            DropForeignKey("dbo.GameCategory", "Game_Id", "dbo.Game");
            DropForeignKey("dbo.GameCategory", "Tag_Name", "dbo.Tag");
            DropForeignKey("dbo.MemberFavoriteTag", "Member_UserId", "dbo.Member");
            DropForeignKey("dbo.MemberFavoriteTag", "Tag_Name", "dbo.Tag");
            DropForeignKey("dbo.MemberFavoritePlatform", "Member_UserId", "dbo.Member");
            DropForeignKey("dbo.MemberFavoritePlatform", "Platform_PlatformCode", "dbo.Platform");
            DropForeignKey("dbo.MemberEventMembership", "Member_UserId", "dbo.Member");
            DropForeignKey("dbo.MemberEventMembership", "Event_Id", "dbo.Event");
            DropForeignKey("dbo.MemberWishlistItem", "MemberId", "dbo.Member");
            DropForeignKey("dbo.MemberWishlistItem", "ProductId", "dbo.Product");
            AddForeignKey("dbo.GameESRBContentDescriptor", "Game_Id", "dbo.Game", "Id");
            AddForeignKey("dbo.GameESRBContentDescriptor", "ESRBContentDescriptor_Id", "dbo.ESRBContentDescriptor", "Id");
            AddForeignKey("dbo.GameCategory", "Game_Id", "dbo.Game", "Id");
            AddForeignKey("dbo.GameCategory", "Tag_Name", "dbo.Tag", "Name");
            AddForeignKey("dbo.MemberFavoriteTag", "Member_UserId", "dbo.Member", "UserId");
            AddForeignKey("dbo.MemberFavoriteTag", "Tag_Name", "dbo.Tag", "Name");
            AddForeignKey("dbo.MemberFavoritePlatform", "Member_UserId", "dbo.Member", "UserId");
            AddForeignKey("dbo.MemberFavoritePlatform", "Platform_PlatformCode", "dbo.Platform", "PlatformCode");
            AddForeignKey("dbo.MemberEventMembership", "Member_UserId", "dbo.Member", "UserId");
            AddForeignKey("dbo.MemberEventMembership", "Event_Id", "dbo.Event", "Id");
            AddForeignKey("dbo.MemberWishlistItem", "MemberId", "dbo.Member", "UserId");
            AddForeignKey("dbo.MemberWishlistItem", "ProductId", "dbo.Product", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.MemberWishlistItem", "ProductId", "dbo.Product");
            DropForeignKey("dbo.MemberWishlistItem", "MemberId", "dbo.Member");
            DropForeignKey("dbo.MemberEventMembership", "Event_Id", "dbo.Event");
            DropForeignKey("dbo.MemberEventMembership", "Member_UserId", "dbo.Member");
            DropForeignKey("dbo.MemberFavoritePlatform", "Platform_PlatformCode", "dbo.Platform");
            DropForeignKey("dbo.MemberFavoritePlatform", "Member_UserId", "dbo.Member");
            DropForeignKey("dbo.MemberFavoriteTag", "Tag_Name", "dbo.Tag");
            DropForeignKey("dbo.MemberFavoriteTag", "Member_UserId", "dbo.Member");
            DropForeignKey("dbo.GameCategory", "Tag_Name", "dbo.Tag");
            DropForeignKey("dbo.GameCategory", "Game_Id", "dbo.Game");
            DropForeignKey("dbo.GameESRBContentDescriptor", "ESRBContentDescriptor_Id", "dbo.ESRBContentDescriptor");
            DropForeignKey("dbo.GameESRBContentDescriptor", "Game_Id", "dbo.Game");
            AddForeignKey("dbo.MemberWishlistItem", "ProductId", "dbo.Product", "Id", cascadeDelete: true);
            AddForeignKey("dbo.MemberWishlistItem", "MemberId", "dbo.Member", "UserId", cascadeDelete: true);
            AddForeignKey("dbo.MemberEventMembership", "Event_Id", "dbo.Event", "Id", cascadeDelete: true);
            AddForeignKey("dbo.MemberEventMembership", "Member_UserId", "dbo.Member", "UserId", cascadeDelete: true);
            AddForeignKey("dbo.MemberFavoritePlatform", "Platform_PlatformCode", "dbo.Platform", "PlatformCode", cascadeDelete: true);
            AddForeignKey("dbo.MemberFavoritePlatform", "Member_UserId", "dbo.Member", "UserId", cascadeDelete: true);
            AddForeignKey("dbo.MemberFavoriteTag", "Tag_Name", "dbo.Tag", "Name", cascadeDelete: true);
            AddForeignKey("dbo.MemberFavoriteTag", "Member_UserId", "dbo.Member", "UserId", cascadeDelete: true);
            AddForeignKey("dbo.GameCategory", "Tag_Name", "dbo.Tag", "Name", cascadeDelete: true);
            AddForeignKey("dbo.GameCategory", "Game_Id", "dbo.Game", "Id", cascadeDelete: true);
            AddForeignKey("dbo.GameESRBContentDescriptor", "ESRBContentDescriptor_Id", "dbo.ESRBContentDescriptor", "Id", cascadeDelete: true);
            AddForeignKey("dbo.GameESRBContentDescriptor", "Game_Id", "dbo.Game", "Id", cascadeDelete: true);
        }
    }
}
