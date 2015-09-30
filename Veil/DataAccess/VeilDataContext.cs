using System.Data.Entity;
using Veil.Models;

namespace Veil.DataAccess
{
    public class VeilDataContext : DbContext
    {
        public VeilDataContext() : base("name=VeilDatabase") { }

        public DbSet<Member> Members { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>().HasMany(p => p.GameProducts).WithRequired(gp => gp.Developer).HasForeignKey(gp => gp.DeveloperId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Company>().HasMany(p => p.GameProducts).WithRequired(gp => gp.Publisher).HasForeignKey(gp => gp.PublisherId).WillCascadeOnDelete(false);

            modelBuilder.Entity<Member>().
                HasMany(m => m.WishList).
                WithMany().
                Map(t => t.MapLeftKey("MemberId").
                    MapRightKey("ProductId").
                    ToTable("MemberWishlist"));

/*            modelBuilder.Entity<GameProduct>().
                HasRequired(gp => gp.Developer).
                WithMany(c => c.GameProducts).
                HasForeignKey(gp => gp.DeveloperId).
                WillCascadeOnDelete(true);

            modelBuilder.Entity<GameProduct>().
                HasRequired(c => c.Publisher).
                WithMany(c => c.GameProducts).
                HasForeignKey(gp => gp.PublisherId).
                WillCascadeOnDelete(true);*/

/*            modelBuilder.Entity<GameProduct>().
                HasRequired(gp => gp.Game).
                WithMany(g => g.GameProducts).
                HasForeignKey(gp => gp.GameId);*/

            /*modelBuilder.Entity<WebOrder>().
                HasRequired(wo => wo.OrderItems).
                WithMany().
                HasForeignKey(wo => wo.Id).
                WillCascadeOnDelete(false);*/

            /*modelBuilder.Entity<MemberPreferences>().
                HasRequired(mp => mp.Member).
                WithOptional(m => m.Preferences).
                WillCascadeOnDelete(true);*/

            base.OnModelCreating(modelBuilder);
        }
    }
}
