using MP.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace MP.Permissions;

public class MPPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(MPPermissions.GroupName);

        //Define your own permissions here. Example:
        //myGroup.AddPermission(MPPermissions.MyPermission1, L("Permission:MyPermission1"));
        var boothsPermission = myGroup.AddPermission(MPPermissions.Booths.Default, L("Permission:Booths"));
        boothsPermission.AddChild(MPPermissions.Booths.Create, L("Permission:Booths.Create"));
        boothsPermission.AddChild(MPPermissions.Booths.Edit, L("Permission:Booths.Edit"));
        boothsPermission.AddChild(MPPermissions.Booths.Delete, L("Permission:Booths.Delete"));
        boothsPermission.AddChild(MPPermissions.Booths.ManageSettings, L("Permission:Booths.ManageSettings"));
        boothsPermission.AddChild(MPPermissions.Booths.ManualReservation, L("Permission:Booths.ManualReservation"));

        var boothTypesPermission = myGroup.AddPermission(MPPermissions.BoothTypes.Default, L("Permission:BoothTypes"));
        boothTypesPermission.AddChild(MPPermissions.BoothTypes.Create, L("Permission:BoothTypes.Create"));
        boothTypesPermission.AddChild(MPPermissions.BoothTypes.Edit, L("Permission:BoothTypes.Edit"));
        boothTypesPermission.AddChild(MPPermissions.BoothTypes.Delete, L("Permission:BoothTypes.Delete"));
        boothTypesPermission.AddChild(MPPermissions.BoothTypes.ManageTypes, L("Permission:BoothTypes.ManageTypes"));

        var floorPlansPermission = myGroup.AddPermission(MPPermissions.FloorPlans.Default, L("Permission:FloorPlans"));
        floorPlansPermission.AddChild(MPPermissions.FloorPlans.Create, L("Permission:FloorPlans.Create"));
        floorPlansPermission.AddChild(MPPermissions.FloorPlans.Edit, L("Permission:FloorPlans.Edit"));
        floorPlansPermission.AddChild(MPPermissions.FloorPlans.Delete, L("Permission:FloorPlans.Delete"));
        floorPlansPermission.AddChild(MPPermissions.FloorPlans.Publish, L("Permission:FloorPlans.Publish"));
        floorPlansPermission.AddChild(MPPermissions.FloorPlans.Design, L("Permission:FloorPlans.Design"));

        var rentalsPermission = myGroup.AddPermission(MPPermissions.Rentals.Default, L("Permission:Rentals"));
        rentalsPermission.AddChild(MPPermissions.Rentals.Create, L("Permission:Rentals.Create"));
        rentalsPermission.AddChild(MPPermissions.Rentals.Edit, L("Permission:Rentals.Edit"));
        rentalsPermission.AddChild(MPPermissions.Rentals.Delete, L("Permission:Rentals.Delete"));
        rentalsPermission.AddChild(MPPermissions.Rentals.Manage, L("Permission:Rentals.Manage"));
        rentalsPermission.AddChild(MPPermissions.Rentals.Extend, L("Permission:Rentals.Extend"));

        // Dashboard permissions
        var dashboardPermission = myGroup.AddPermission(MPPermissions.Dashboard.Default, L("Permission:Dashboard"));
        dashboardPermission.AddChild(MPPermissions.Dashboard.ViewSalesAnalytics, L("Permission:Dashboard.ViewSalesAnalytics"));
        dashboardPermission.AddChild(MPPermissions.Dashboard.ViewFinancialReports, L("Permission:Dashboard.ViewFinancialReports"));
        dashboardPermission.AddChild(MPPermissions.Dashboard.ViewBoothOccupancy, L("Permission:Dashboard.ViewBoothOccupancy"));
        dashboardPermission.AddChild(MPPermissions.Dashboard.ViewCustomerAnalytics, L("Permission:Dashboard.ViewCustomerAnalytics"));
        dashboardPermission.AddChild(MPPermissions.Dashboard.ExportReports, L("Permission:Dashboard.ExportReports"));

        // Payment Providers permissions
        var paymentProvidersPermission = myGroup.AddPermission(MPPermissions.PaymentProviders.Default, L("Permission:PaymentProviders"));
        paymentProvidersPermission.AddChild(MPPermissions.PaymentProviders.Manage, L("Permission:PaymentProviders.Manage"));

        // Customer Dashboard permissions - available for all authenticated users to manage their own data
        var customerDashboardPermission = myGroup.AddPermission(
            MPPermissions.CustomerDashboard.Default,
            L("Permission:CustomerDashboard"),
            multiTenancySide: MultiTenancySides.Both);
        customerDashboardPermission.AddChild(MPPermissions.CustomerDashboard.ViewDashboard, L("Permission:CustomerDashboard.ViewDashboard"));
        customerDashboardPermission.AddChild(MPPermissions.CustomerDashboard.ManageMyItems, L("Permission:CustomerDashboard.ManageMyItems"));
        customerDashboardPermission.AddChild(MPPermissions.CustomerDashboard.ManageMyRentals, L("Permission:CustomerDashboard.ManageMyRentals"));
        customerDashboardPermission.AddChild(MPPermissions.CustomerDashboard.ViewStatistics, L("Permission:CustomerDashboard.ViewStatistics"));
        customerDashboardPermission.AddChild(MPPermissions.CustomerDashboard.RequestSettlement, L("Permission:CustomerDashboard.RequestSettlement"));
        customerDashboardPermission.AddChild(MPPermissions.CustomerDashboard.ViewNotifications, L("Permission:CustomerDashboard.ViewNotifications"));

        // Chat permissions - customer support chat between customers and admins/sellers
        var chatPermission = myGroup.AddPermission(
            MPPermissions.Chat.Default,
            L("Permission:Chat"),
            multiTenancySide: MultiTenancySides.Both);
        chatPermission.AddChild(MPPermissions.Chat.ManageCustomerChats, L("Permission:Chat.ManageCustomerChats"));

        // Organizational Units permissions
        var organizationalUnitsPermission = myGroup.AddPermission(MPPermissions.OrganizationalUnits.Default, L("Permission:OrganizationalUnits"));
        organizationalUnitsPermission.AddChild(MPPermissions.OrganizationalUnits.ManageUsers, L("Permission:OrganizationalUnits.ManageUsers"));

        // Tenant settings permissions
        var tenantPermission = myGroup.AddPermission(MPPermissions.Tenant.Default, L("Permission:Tenant"));
        tenantPermission.AddChild(MPPermissions.Tenant.ManageCurrency, L("Permission:Tenant.ManageCurrency"));
        tenantPermission.AddChild(MPPermissions.Tenant.ManageOrganizationalUnits, L("Permission:Tenant.ManageOrganizationalUnits"));

        // Promotions permissions
        var promotionsPermission = myGroup.AddPermission(MPPermissions.Promotions.Default, L("Permission:Promotions"));
        promotionsPermission.AddChild(MPPermissions.Promotions.Create, L("Permission:Promotions.Create"));
        promotionsPermission.AddChild(MPPermissions.Promotions.Edit, L("Permission:Promotions.Edit"));
        promotionsPermission.AddChild(MPPermissions.Promotions.Delete, L("Permission:Promotions.Delete"));
        promotionsPermission.AddChild(MPPermissions.Promotions.Manage, L("Permission:Promotions.Manage"));

        // HomePage Content Management permissions
        var homePageContentPermission = myGroup.AddPermission(MPPermissions.HomePageContent.Default, L("Permission:HomePageContent"));
        homePageContentPermission.AddChild(MPPermissions.HomePageContent.Create, L("Permission:HomePageContent.Create"));
        homePageContentPermission.AddChild(MPPermissions.HomePageContent.Edit, L("Permission:HomePageContent.Edit"));
        homePageContentPermission.AddChild(MPPermissions.HomePageContent.Delete, L("Permission:HomePageContent.Delete"));
        homePageContentPermission.AddChild(MPPermissions.HomePageContent.Manage, L("Permission:HomePageContent.Manage"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<MPResource>(name);
    }
}
