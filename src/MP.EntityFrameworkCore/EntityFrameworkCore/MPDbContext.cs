using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using MP.Domain.Booths;
using MP.Domain.BoothTypes;
using MP.Domain.Rentals;
using MP.Domain.FloorPlans;
using MP.Domain.Payments;
using MP.Domain.Carts;
using MP.Domain.Terminals;
using MP.Domain.FiscalPrinters;
using MP.Domain.Notifications;
using MP.Domain.Settlements;
using MP.Domain.Chat;
using MP.Domain.Items;
using MP.Domain.Promotions;
using MP.Domain.Identity;
using MP.Domain.HomePageContent;
using MP.Domain.Files;

namespace MP.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class MPDbContext :
    AbpDbContext<MPDbContext>,
    ITenantManagementDbContext,
    IIdentityDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */


    #region Entities from the modules

    /* Notice: We only implemented IIdentityProDbContext and ISaasDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityProDbContext and ISaasDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    // Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }

    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }
    public DbSet<Booth> Booths { get; set; }
    public DbSet<BoothType> BoothTypes { get; set; }
    public DbSet<Rental> Rentals { get; set; }
    public DbSet<RentalExtensionPayment> RentalExtensionPayments { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<FloorPlan> FloorPlans { get; set; }
    public DbSet<FloorPlanBooth> FloorPlanBooths { get; set; }
    public DbSet<FloorPlanElement> FloorPlanElements { get; set; }
    public DbSet<P24Transaction> P24Transactions { get; set; }
    public DbSet<StripeTransaction> StripeTransactions { get; set; }
    public DbSet<PayPalTransaction> PayPalTransactions { get; set; }
    public DbSet<TenantTerminalSettings> TenantTerminalSettings { get; set; }
    public DbSet<TenantFiscalPrinterSettings> TenantFiscalPrinterSettings { get; set; }
    public DbSet<UserNotification> UserNotifications { get; set; }
    public DbSet<Settlement> Settlements { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<ItemSheet> ItemSheets { get; set; }
    public DbSet<ItemSheetItem> ItemSheetItems { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<PromotionUsage> PromotionUsages { get; set; }
    public DbSet<HomePageSection> HomePageSections { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<UploadedFile> UploadedFiles { get; set; }

    #endregion

    public MPDbContext(DbContextOptions<MPDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureTenantManagement();
        builder.ConfigureBlobStoring();

        /* Configure your own tables/entities inside here */

        //builder.Entity<YourEntity>(b =>
        //{
        //    b.ToTable(MPConsts.DbTablePrefix + "YourEntities", MPConsts.DbSchema);
        //    b.ConfigureByConvention(); //auto configure for the base class props
        //    //...
        //});

        builder.ConfigureMP();
    }
}
