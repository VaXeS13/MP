namespace MP.Domain.FloorPlans
{
    public enum FloorPlanElementType
    {
        // Architectural/Structural elements
        Wall = 1,           // Ściana - separates spaces
        Door = 2,           // Drzwi - entrance/exit
        Window = 3,         // Okno - architectural feature
        Pillar = 4,         // Kolumna - structural obstacle (round or rectangular)
        Stairs = 5,         // Schody - inter-floor communication

        // Functional elements
        Checkout = 6,       // Kasa - payment point for customers
        Restroom = 7,       // Toaleta - WC (male/female/universal)
        InfoDesk = 8,       // Punkt informacyjny - reception/information
        EmergencyExit = 9,  // Wyjście awaryjne - safety/emergency exit
        Storage = 10,       // Magazyn/Zaplecze - storage area

        // Helper elements
        TextLabel = 11,     // Etykieta tekstowa - arbitrary text description
        Zone = 12           // Strefa/Obszar - semi-transparent area to mark sections
    }
}
