using EfEnumToLookup.LookupGenerator;
using Veil.DataModels.Models;

namespace Veil.DataAccess.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<VeilDataContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            ContextKey = "Veil.DataAccess.VeilDataContext";
        }

        protected override void Seed(VeilDataContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data.

            context.ESRBRatings.AddOrUpdate(
                er => er.RatingId,
                new ESRBRating
                {
                    RatingId = "EC",
                    Description = "Early Childhood"
                },
                new ESRBRating
                {
                    RatingId = "E",
                    Description = "Everyone"
                },
                new ESRBRating
                {
                    RatingId = "E10+",
                    Description = "Everyone 10+"
                },
                new ESRBRating
                {
                    RatingId = "T",
                    Description = "Teen"
                },
                new ESRBRating
                {
                    RatingId = "M",
                    Description = "Mature"
                },
                new ESRBRating
                {
                    RatingId = "AO",
                    Description = "Adults Only"
                },
                new ESRBRating
                {
                    RatingId = "RP",
                    Description = "Rating Pending"
                }
            );

            context.ESRBContentDescriptors.AddOrUpdate(
                ecd => ecd.Id,
                new ESRBContentDescriptor { DescriptorName = "Alcohol Reference" },
                new ESRBContentDescriptor { DescriptorName = "Animated Blood" },
                new ESRBContentDescriptor { DescriptorName = "Blood" },
                new ESRBContentDescriptor { DescriptorName = "Blood and Gore" },
                new ESRBContentDescriptor { DescriptorName = "Cartoon Violence" },
                new ESRBContentDescriptor { DescriptorName = "Comic Mischief" },
                new ESRBContentDescriptor { DescriptorName = "Crude Humor" },
                new ESRBContentDescriptor { DescriptorName = "Drug Reference" },
                new ESRBContentDescriptor { DescriptorName = "Fantasy Violence" },
                new ESRBContentDescriptor { DescriptorName = "Intense Violence" },
                new ESRBContentDescriptor { DescriptorName = "Language" },
                new ESRBContentDescriptor { DescriptorName = "Lyrics" },
                new ESRBContentDescriptor { DescriptorName = "Mature Humor" },
                new ESRBContentDescriptor { DescriptorName = "Nudity" },
                new ESRBContentDescriptor { DescriptorName = "Partial Nudity" },
                new ESRBContentDescriptor { DescriptorName = "Real Gambling" },
                new ESRBContentDescriptor { DescriptorName = "Sexual Content" },
                new ESRBContentDescriptor { DescriptorName = "Sexual Themes" },
                new ESRBContentDescriptor { DescriptorName = "Sexual Violence" },
                new ESRBContentDescriptor { DescriptorName = "Simulated Gambling" },
                new ESRBContentDescriptor { DescriptorName = "Strong Language" },
                new ESRBContentDescriptor { DescriptorName = "Strong Lyrics" },
                new ESRBContentDescriptor { DescriptorName = "Strong Sexual Content" },
                new ESRBContentDescriptor { DescriptorName = "Suggestive Themes" },
                new ESRBContentDescriptor { DescriptorName = "Tobacco Reference" },
                new ESRBContentDescriptor { DescriptorName = "Use of Alcohol" },
                new ESRBContentDescriptor { DescriptorName = "Use of Drugs" },
                new ESRBContentDescriptor { DescriptorName = "Use of Tobacco" },
                new ESRBContentDescriptor { DescriptorName = "Violence" },
                new ESRBContentDescriptor { DescriptorName = "Violent References" }
            );

            /* Enum to Lookup Tables Setup */
            EnumToLookup enumToLookup = new EnumToLookup
            {
                TableNamePrefix = "",
                TableNameSuffix = "_Lookup",
                NameFieldLength = 64
            };

            enumToLookup.Apply(context);
        }
    }
}
