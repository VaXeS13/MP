using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP.Rentals
{
    public enum RentalStatus
    {
        Draft = 0,      // Projekt (nie opłacone)
        Active = 1,     // Aktywne wynajęcie
        Expired = 2,    // Wygasłe
        Cancelled = 3,  // Anulowane
        Extended = 4    // Przedłużone
    }
}
