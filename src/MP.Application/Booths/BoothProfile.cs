using AutoMapper;
using MP.Domain.Booths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP.Booths
{
    public class BoothProfile : Profile
    {
        public BoothProfile()
        {
            // Mapowanie z Entity na DTOs
            CreateMap<Booth, BoothDto>()
                .ForMember(dest => dest.StatusDisplayName,
                    opt => opt.MapFrom(src => GetBoothStatusDisplayName(src.Status)));

            CreateMap<Booth, BoothListDto>()
                .ForMember(dest => dest.StatusDisplayName,
                    opt => opt.MapFrom(src => GetBoothStatusDisplayName(src.Status)));
        }

        private static string GetBoothStatusDisplayName(BoothStatus status)
        {
            return status switch
            {
                BoothStatus.Available => "Dostępne",
                BoothStatus.Reserved => "Zarezerwowane",
                BoothStatus.Rented => "Wynajęte",
                BoothStatus.Maintenance => "Konserwacja",
                _ => status.ToString()
            };
        }
    }
}