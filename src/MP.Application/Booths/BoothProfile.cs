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
                    opt => opt.MapFrom(src => GetBoothStatusDisplayName(src.Status)))
                .ForMember(dest => dest.CurrencyDisplayName,
                    opt => opt.MapFrom(src => GetCurrencyDisplayName(src.Currency)));

            CreateMap<Booth, BoothListDto>()
                .ForMember(dest => dest.StatusDisplayName,
                    opt => opt.MapFrom(src => GetBoothStatusDisplayName(src.Status)))
                .ForMember(dest => dest.CurrencyDisplayName,
                    opt => opt.MapFrom(src => GetCurrencyDisplayName(src.Currency)));
        }

        private static string GetCurrencyDisplayName(Currency currency)
        {
            return currency switch
            {
                (Currency)0 => "PLN", // TEMPORARY FIX: treat invalid 0 as PLN until migration runs
                Currency.PLN => "PLN",
                Currency.USD => "USD",
                Currency.EUR => "EUR",
                Currency.GBP => "GBP",
                Currency.CZK => "CZK",
                _ => currency.ToString()
            };
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