using Veil.DataModels.Models;

namespace Veil.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;
    
    /// <summary>
    ///     Creates a trigger which places the lesser of the two MemberId's first to 
    ///     ensure uniqueness across the combination of the PK columns
    /// </summary>
    public partial class Add_Friendship_InsertTrigger : DbMigration
    {
        private const string FriendshipInsertTriggerName = "Friendship_Insert_OrderedKeys";

        public override void Up()
        {
            Sql($@"CREATE TRIGGER [{VeilDataContext.SCHEMA_NAME}].[{FriendshipInsertTriggerName}]
                ON [{VeilDataContext.SCHEMA_NAME}].[{nameof(Friendship)}]
                INSTEAD OF INSERT
                AS
                BEGIN
	                INSERT INTO {nameof(Friendship)}
	                SELECT 
		                -- Ensure the lesser of the two values is first.
		                CASE WHEN ({nameof(Friendship.ReceiverId)} < {nameof(Friendship.RequesterId)}) THEN
			                {nameof(Friendship.ReceiverId)}
		                ELSE
			                {nameof(Friendship.RequesterId)}
		                END,
		                -- Ensure the greater of the two values is second
		                CASE WHEN ({nameof(Friendship.ReceiverId)} < {nameof(Friendship.RequesterId)}) THEN
			                {nameof(Friendship.RequesterId)}
		                ELSE
			                {nameof(Friendship.ReceiverId)}
		                END,
		                {nameof(Friendship.RequestStatus)}
	                FROM inserted
                END;"
                );
        }
        
        public override void Down()
        {
            Sql($@"DROP TRIGGER [{VeilDataContext.SCHEMA_NAME}].[{FriendshipInsertTriggerName}]");
        }
    }
}
