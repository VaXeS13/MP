using System;

namespace MP.Rentals
{
    public enum ExtensionPaymentType
    {
        Free = 0,      // Gratis - za darmo, natychmiast, koszt 0
        Cash = 1,      // Gotówka - płatność na miejscu, natychmiast, oznaczone jako opłacone
        Terminal = 2,  // Terminal - płatność terminalem na miejscu, z transaction ID i receipt
        Online = 3     // Koszyk - dodanie do koszyka z timeout, gap validation pomijane
    }
}
