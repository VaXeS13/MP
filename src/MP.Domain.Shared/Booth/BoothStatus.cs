using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP.Domain.Booths
{
    public enum BoothStatus
    {
        Available = 1,    // Dostępne do wynajęcia
        Reserved = 2,     // Zarezerwowane (oczekuje na płatność)
        Rented = 3,       // Wynajęte
        Maintenance = 4   // W trakcie konserwacji
    }
}
