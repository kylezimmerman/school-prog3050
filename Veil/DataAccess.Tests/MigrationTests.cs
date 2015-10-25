using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public class MigrationTests
{
    [Test]
    public void RunAll()
    {
        var configuration = new Veil.DataAccess.Migrations.Configuration();
        var migrator = new DbMigrator(configuration);

        List<string> migrations = new List<string> { "0" };
        migrations.AddRange(migrator.GetLocalMigrations());

        // Ensure we are starting from pre-first migration state
        migrator.Update(migrations.First());

        // Run all the migrations. Goes up one, down one, up two, down one, up two, down one, etc.
        for (int i = 0; i < migrations.Count; i++)
        {
            migrator.Update(migrations[i]);

            if (i > 0)
            {
                migrator.Update(migrations[i - 1]);
            }
        }
    }

    [Test]
    public void FullUp_FullDown()
    {
        var configuration = new Veil.DataAccess.Migrations.Configuration();
        var migrator = new DbMigrator(configuration);

        List<string> migrations = new List<string> { "0" };
        migrations.AddRange(migrator.GetLocalMigrations());

        migrator.Update(migrations.Last());
        migrator.Update(migrations.First());
    }
}