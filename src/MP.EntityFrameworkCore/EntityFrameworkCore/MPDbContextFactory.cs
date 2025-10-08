using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MP.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class MPDbContextFactory : IDesignTimeDbContextFactory<MPDbContext>
{
    public MPDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();

        MPEfCoreEntityExtensionMappings.Configure();

        var builder = new DbContextOptionsBuilder<MPDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));

        return new MPDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../MP.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}
