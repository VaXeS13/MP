namespace MP.Localization
{
    /// <summary>
    /// Maps notification type constants to Polish display names.
    /// Used for both backend and frontend localization.
    /// </summary>
    public static class NotificationTypeNames
    {
        /// <summary>
        /// Gets the Polish display name for a notification type.
        /// </summary>
        /// <param name="notificationType">The notification type constant (e.g., "PaymentReceived")</param>
        /// <returns>Polish display name (e.g., "Płatność rozpoczęta")</returns>
        public static string GetDisplayName(string notificationType)
        {
            return notificationType switch
            {
                // Payment related
                NotificationTypes.PaymentReceived => "Płatność rozpoczęta",
                NotificationTypes.PaymentFailed => "Błąd płatności",

                // Rental related
                NotificationTypes.RentalStarted => "Wynajem rozpoczęty",
                NotificationTypes.RentalCompleted => "Wynajem zakończony",
                NotificationTypes.RentalExtending => "Przedłużenie wynajmu",
                NotificationTypes.RentalExtended => "Wynajem przedłużony",
                NotificationTypes.RentalExpiring => "Wynajem wkrótce wygasa",
                NotificationTypes.RentalExpired => "Wynajem wygasł",

                // Item related
                NotificationTypes.ItemSold => "Przedmiot sprzedany",
                NotificationTypes.ItemExpiring => "Przedmiot wkrótce wygasa",

                // Settlement related
                NotificationTypes.SettlementReady => "Rozliczenie gotowe",
                NotificationTypes.SettlementPaid => "Rozliczenie wypłacone",

                // System
                NotificationTypes.SystemAnnouncement => "Ogłoszenie systemowe",

                // Default fallback
                _ => notificationType
            };
        }
    }

    /// <summary>
    /// Constants for notification types used throughout the system.
    /// Must match the Type field in UserNotification entity.
    /// </summary>
    public static class NotificationTypes
    {
        public const string PaymentReceived = "PaymentReceived";
        public const string PaymentFailed = "PaymentFailed";
        public const string RentalStarted = "RentalStarted";
        public const string RentalCompleted = "RentalCompleted";
        public const string RentalExtending = "RentalExtending";
        public const string RentalExtended = "RentalExtended";
        public const string RentalExpiring = "RentalExpiring";
        public const string RentalExpired = "RentalExpired";
        public const string ItemSold = "ItemSold";
        public const string ItemExpiring = "ItemExpiring";
        public const string SettlementReady = "SettlementReady";
        public const string SettlementPaid = "SettlementPaid";
        public const string SystemAnnouncement = "SystemAnnouncement";
    }
}
