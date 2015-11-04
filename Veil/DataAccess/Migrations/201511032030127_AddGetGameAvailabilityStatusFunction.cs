using Veil.DataModels.Models;

namespace Veil.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;
    
    /// <summary>
    ///     Creates a store function which computes the AvailabilityStatus of for a Game
    /// </summary>
    public partial class Add_GetGameAvailabilityStatus_Function : DbMigration
    {
        public override void Up()
        {
            Sql($@"CREATE FUNCTION [{VeilDataContext.SCHEMA_NAME}].[{VeilDataContext.GET_GAME_AVAILABILITY_STATUS_FUNCTION_NAME}] 
                ( 
                    @GameId_IN uniqueidentifier
                )
                RETURNS int
                AS
                BEGIN
                    DECLARE @Result int

                    DECLARE @PreOrder int = {(int)AvailabilityStatus.PreOrder}
                    DECLARE @Available int = {(int)AvailabilityStatus.Available}
                    DECLARE @DiscontinuedByManufacturer int = {(int)AvailabilityStatus.DiscontinuedByManufacturer}
                    DECLARE @NotForSale int = {(int)AvailabilityStatus.NotForSale}

                    IF NOT EXISTS(SELECT 1
                                   FROM Product AS p INNER JOIN GameProduct AS gp
                                       ON p.Id = gp.Id
                                   WHERE gp.GameId = @GameId_IN)
                        BEGIN
                            SELECT @Result = @NotForSale
                        END
                    ELSE IF EXISTS(SELECT 1
                                   FROM Product AS p INNER JOIN GameProduct AS gp
                                       ON p.Id = gp.Id
                                   WHERE gp.GameId = @GameId_IN AND p.ProductAvailabilityStatus = @PreOrder)
                        BEGIN
                            SELECT @Result = @PreOrder
                        END
                    ELSE IF EXISTS(SELECT 1
                                   FROM Product AS p INNER JOIN GameProduct AS gp
                                       ON p.Id = gp.Id
                                   WHERE gp.GameId = @GameId_IN AND p.ProductAvailabilityStatus = @Available)
                        BEGIN
                            SELECT @Result = @Available
                        END
                    ELSE IF EXISTS(SELECT 1
                                   FROM Product AS p INNER JOIN GameProduct AS gp
                                       ON p.Id = gp.Id
                                   WHERE gp.GameId = @GameId_IN AND p.ProductAvailabilityStatus = @DiscontinuedByManufacturer)
                        BEGIN
                            SELECT @Result = @DiscontinuedByManufacturer
                        END
                    ELSE
                        BEGIN
                            SELECT @Result = @NotForSale
                        END

                    RETURN @Result;
                END",
                suppressTransaction: true);

            Sql($@"ALTER TABLE [{VeilDataContext.SCHEMA_NAME}].[{nameof(Game)}] 
                    ADD [{nameof(Game.GameAvailabilityStatus)}]
                        AS ([{VeilDataContext.SCHEMA_NAME}].[{VeilDataContext.GET_GAME_AVAILABILITY_STATUS_FUNCTION_NAME}]([{nameof(Game.Id)}]))");
        }
        
        public override void Down()
        {
            DropColumn("dbo.Game", "GameAvailabilityStatus");
            Sql("DROP FUNCTION [" + VeilDataContext.SCHEMA_NAME + "].[" + VeilDataContext.GET_GAME_AVAILABILITY_STATUS_FUNCTION_NAME + "]");
        }
    }
}
