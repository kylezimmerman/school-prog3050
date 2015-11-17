namespace Veil.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddPhysicalGameProductSkuSequence : DbMigration
    {
        public override void Up()
        {
            Sql($"CREATE SEQUENCE {VeilDataContext.SCHEMA_NAME}.{VeilDataContext.PHYSICAL_GAME_PRODUCT_SKU_SEQUENCE_NAME} " +
                "AS bigint " +
                "INCREMENT BY 1 " +
                "MINVALUE 100 " +
                "MAXVALUE 999999999999 " +
                "NO CYCLE " +
                "CACHE 20");
        }

        public override void Down()
        {
            Sql($"DROP SEQUENCE {VeilDataContext.SCHEMA_NAME}.{VeilDataContext.PHYSICAL_GAME_PRODUCT_SKU_SEQUENCE_NAME}");
        }
    }
}
