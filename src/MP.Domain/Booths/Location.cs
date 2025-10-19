using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;

namespace MP.Domain.Booths
{
    public class Location : Entity<Guid>
    {
        public required string Name { get; set; }
        public int TotalBooths { get; set; }
    }
}
