using System;
using System.ComponentModel.DataAnnotations;

namespace MP.Items
{
    public class AssignSheetToRentalDto
    {
        [Required]
        public Guid RentalId { get; set; }
    }
}
