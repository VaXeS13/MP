namespace MP.Permissions;

public static class MPPermissions
{
    public const string GroupName = "MP";
    public static class Booths
    {
        public const string Default = GroupName + ".Booths";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string ManageSettings = Default + ".ManageSettings"; // Manage booth-related settings
        public const string ManualReservation = Default + ".ManualReservation"; // Manually create reservations for users
    }

    public static class BoothTypes
    {
        public const string Default = GroupName + ".BoothTypes";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string ManageTypes = Default + ".ManageTypes"; // Definiowanie typów stanowisk
    }

    public static class FloorPlans
    {
        public const string Default = GroupName + ".FloorPlans";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Publish = Default + ".Publish";
        public const string Design = Default + ".Design"; // Projektowanie planów pięter
    }

    public static class Rentals
    {
        public const string Default = GroupName + ".Rentals";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Manage = Default + ".Manage"; // Pay, Start, Complete, Cancel
        public const string Extend = Default + ".Extend"; // Extend rental period (admin/seller only)
    }


    public static class Dashboard
    {
        public const string Default = GroupName + ".Dashboard";
        public const string ViewSalesAnalytics = Default + ".ViewSalesAnalytics";
        public const string ViewFinancialReports = Default + ".ViewFinancialReports";
        public const string ViewBoothOccupancy = Default + ".ViewBoothOccupancy";
        public const string ViewCustomerAnalytics = Default + ".ViewCustomerAnalytics";
        public const string ExportReports = Default + ".ExportReports";
    }

    public static class PaymentProviders
    {
        public const string Default = GroupName + ".PaymentProviders";
        public const string Manage = Default + ".Manage"; // Zarządzanie ustawieniami dostawców płatności
    }

    // Customer Dashboard permissions - for authenticated users managing their own data
    public static class CustomerDashboard
    {
        public const string Default = GroupName + ".CustomerDashboard";
        public const string ViewDashboard = Default + ".ViewDashboard";
        public const string ManageMyItems = Default + ".ManageMyItems";
        public const string ManageMyRentals = Default + ".ManageMyRentals";
        public const string ViewStatistics = Default + ".ViewStatistics";
        public const string RequestSettlement = Default + ".RequestSettlement";
        public const string ViewNotifications = Default + ".ViewNotifications";
    }

    // Chat permissions - for customer support chat
    public static class Chat
    {
        public const string Default = GroupName + ".Chat";
        public const string ManageCustomerChats = Default + ".ManageCustomerChats"; // Admin/Seller can manage all customer chats
    }

    // Tenant settings permissions
    public static class Tenant
    {
        public const string Default = GroupName + ".Tenant";
        public const string ManageCurrency = Default + ".ManageCurrency"; // Manage tenant currency settings
    }

    // Promotions permissions
    public static class Promotions
    {
        public const string Default = GroupName + ".Promotions";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Manage = Default + ".Manage"; // Activate/Deactivate promotions
    }

    //Add your own permission names. Example:
    //public const string MyPermission1 = GroupName + ".MyPermission1";
}
