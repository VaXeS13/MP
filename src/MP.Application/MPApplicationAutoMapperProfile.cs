using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using MP.Application.Contracts.BoothTypes;
using MP.Application.Contracts.Terminals;
using MP.Application.Contracts.CustomerDashboard;
using MP.Booths;
using MP.Domain.Booths;
using MP.Domain.BoothTypes;
using MP.Domain.Rentals;
using MP.Domain.FloorPlans;
using MP.Domain.Terminals;
using MP.Domain.Notifications;
using MP.Domain.Settlements;
using MP.Domain.Items;
using MP.Domain.Promotions;
using MP.Domain.HomePageContent;
using MP.Domain.Files;
using MP.Rentals;
using MP.FloorPlans;
using MP.Items;
using MP.Promotions;
using MP.Application.Contracts.HomePageContent;
using MP.Application.Contracts.Files;
using MP.Application.Contracts.Notifications;
using MP.Domain.Payments;
using MP.Payments;
using MP.Domain.Carts;
using MP.Carts;

namespace MP;

public class MPApplicationAutoMapperProfile : Profile
{
    public MPApplicationAutoMapperProfile()
    {
        /*CreateMap<Booth, BoothDto>()
            .ForMember(dest => dest.TotalAmount,
                opt => opt.MapFrom(src => src.Payment.TotalAmount))
            .ForMember(dest => dest.PaidAmount,
                opt => opt.MapFrom(src => src.Payment.PaidAmount))
            .ForMember(dest => dest.IsPaid,
                opt => opt.MapFrom(src => src.Payment.IsPaid))
            .ForMember(dest => dest.ItemsCount,
                opt => opt.MapFrom(src => src.GetItemsCount()))
            .ForMember(dest => dest.SoldItemsCount,
                opt => opt.MapFrom(src => src.GetSoldItemsCount()));*/

        // BOOTH TYPE MAPPINGS
        CreateMap<BoothType, BoothTypeDto>();
        CreateMap<CreateBoothTypeDto, BoothType>();
        CreateMap<UpdateBoothTypeDto, BoothType>();

        // RENTAL ITEMS MAPPINGS
        // Note: BoothListDto CurrentRental* fields are populated in BoothAppService
        CreateMap<Booth, BoothListDto>()
                .ForMember(dest => dest.StatusDisplayName,
                    opt => opt.MapFrom(src => GetBoothStatusDisplayName(src.Status)))
                .ForMember(dest => dest.CurrentRentalId, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentRentalUserName, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentRentalUserEmail, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentRentalStartDate, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentRentalEndDate, opt => opt.Ignore());

        // NOWE MAPPINGI RENTAL
        CreateMap<Rental, RentalDto>()
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => $"{src.User.Name} {src.User.Surname}"))
            .ForMember(dest => dest.UserEmail,
                opt => opt.MapFrom(src => src.User.Email))
            .ForMember(dest => dest.BoothNumber,
                opt => opt.MapFrom(src => src.Booth.Number))
            .ForMember(dest => dest.StartDate,
                opt => opt.MapFrom(src => src.Period.StartDate))
            .ForMember(dest => dest.EndDate,
                opt => opt.MapFrom(src => src.Period.EndDate))
            .ForMember(dest => dest.DaysCount,
                opt => opt.MapFrom(src => src.Period.GetDaysCount()))
            .ForMember(dest => dest.StatusDisplayName,
                opt => opt.MapFrom(src => GetRentalStatusDisplayName(src.Status)))
            .ForMember(dest => dest.TotalAmount,
                opt => opt.MapFrom(src => src.Payment.TotalAmount))
            .ForMember(dest => dest.Currency,
                opt => opt.MapFrom(src => src.Currency.ToString()))
            .ForMember(dest => dest.PaidAmount,
                opt => opt.MapFrom(src => src.Payment.PaidAmount))
            .ForMember(dest => dest.PaidDate,
                opt => opt.MapFrom(src => src.Payment.PaidDate))
            .ForMember(dest => dest.IsPaid,
                opt => opt.MapFrom(src => src.Payment.IsPaid))
            .ForMember(dest => dest.RemainingAmount,
                opt => opt.MapFrom(src => src.Payment.GetRemainingAmount()))
            .ForMember(dest => dest.ItemsCount,
                opt => opt.MapFrom(src => src.GetItemsCount()))
            .ForMember(dest => dest.SoldItemsCount,
                opt => opt.MapFrom(src => src.GetSoldItemsCount()))
            .ForMember(dest => dest.TotalSalesAmount,
                opt => opt.MapFrom(src => src.GetTotalSalesAmount()))
            .ForMember(dest => dest.TotalCommissionEarned,
                opt => opt.MapFrom(src => src.GetTotalCommissionEarned()));

        CreateMap<Rental, RentalListDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => $"{src.User.Name} {src.User.Surname}"))
            .ForMember(dest => dest.UserEmail,
                opt => opt.MapFrom(src => src.User.Email))
            .ForMember(dest => dest.BoothId, opt => opt.MapFrom(src => src.BoothId))
            .ForMember(dest => dest.BoothNumber,
                opt => opt.MapFrom(src => src.Booth.Number))
            .ForMember(dest => dest.StartDate,
                opt => opt.MapFrom(src => src.Period.StartDate))
            .ForMember(dest => dest.EndDate,
                opt => opt.MapFrom(src => src.Period.EndDate))
            .ForMember(dest => dest.DaysCount,
                opt => opt.MapFrom(src => src.Period.GetDaysCount()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.StatusDisplayName,
                opt => opt.MapFrom(src => GetRentalStatusDisplayName(src.Status)))
            .ForMember(dest => dest.TotalAmount,
                opt => opt.MapFrom(src => src.Payment.TotalAmount))
            .ForMember(dest => dest.PaidAmount,
                opt => opt.MapFrom(src => src.Payment.PaidAmount))
            .ForMember(dest => dest.IsPaid,
                opt => opt.MapFrom(src => src.Payment.IsPaid))
            .ForMember(dest => dest.CreationTime, opt => opt.MapFrom(src => src.CreationTime))
            .ForMember(dest => dest.StartedAt, opt => opt.MapFrom(src => src.StartedAt))
            .ForMember(dest => dest.ItemsCount,
                opt => opt.MapFrom(src => src.GetItemsCount()))
            .ForMember(dest => dest.SoldItemsCount,
                opt => opt.MapFrom(src => src.GetSoldItemsCount()));

        // FLOOR PLAN MAPPINGS
        CreateMap<FloorPlan, FloorPlanDto>();
        CreateMap<CreateFloorPlanDto, FloorPlan>();
        CreateMap<UpdateFloorPlanDto, FloorPlan>();

        // FLOOR PLAN BOOTH MAPPINGS
        CreateMap<FloorPlanBooth, FloorPlanBoothDto>()
            .ForMember(dest => dest.Booth,
                opt => opt.MapFrom(src => src.Booth));
        CreateMap<CreateFloorPlanBoothDto, FloorPlanBooth>();

        // FLOOR PLAN ELEMENT MAPPINGS
        CreateMap<FloorPlanElement, FloorPlanElementDto>();
        CreateMap<CreateFloorPlanElementDto, FloorPlanElement>();
        CreateMap<UpdateFloorPlanElementDto, FloorPlanElement>();

        // BOOTH MAPPINGS (ensure this is present for complete mapping)
        // Note: BoothDto CurrentRental* fields are populated where needed
        CreateMap<Booth, BoothDto>()
            .ForMember(dest => dest.StatusDisplayName,
                opt => opt.MapFrom(src => GetBoothStatusDisplayName(src.Status)))
            .ForMember(dest => dest.CurrentRentalId, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentRentalUserName, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentRentalUserEmail, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentRentalStartDate, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentRentalEndDate, opt => opt.Ignore());

        // TERMINAL SETTINGS MAPPINGS
        CreateMap<TenantTerminalSettings, TerminalSettingsDto>();

        // CUSTOMER DASHBOARD MAPPINGS - Now using ItemSheetItem instead of RentalItem

        CreateMap<UserNotification, CustomerNotificationDto>()
            .ForMember(dest => dest.Title,
                opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Message,
                opt => opt.MapFrom(src => src.Message))
            .ForMember(dest => dest.CreatedAt,
                opt => opt.MapFrom(src => src.CreationTime));

        CreateMap<Settlement, SettlementItemDto>()
            .ForMember(dest => dest.CreatedAt,
                opt => opt.MapFrom(src => src.CreationTime))
            .ForMember(dest => dest.Amount,
                opt => opt.MapFrom(src => src.NetAmount))
            .ForMember(dest => dest.StatusDisplayName,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.ItemsCount,
                opt => opt.MapFrom(src => src.GetItemsCount()))
            .ForMember(dest => dest.TransactionReference,
                opt => opt.MapFrom(src => src.TransactionReference));

        // ITEMS MAPPINGS
        CreateMap<Item, ItemDto>()
            .ForMember(dest => dest.Currency,
                opt => opt.MapFrom(src => src.Currency.ToString()))
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<ItemSheetItem, ItemSheetItemDto>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<ItemSheet, ItemSheetDto>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Items,
                opt => opt.MapFrom(src => src.Items))
            .ForMember(dest => dest.BoothNumber,
                opt => opt.MapFrom(src => src.Rental != null ? src.Rental.Booth.Number : null));

        // CART MAPPINGS
        CreateMap<CartItem, CartItemDto>()
            .ForMember(dest => dest.DaysCount,
                opt => opt.MapFrom(src => src.GetDaysCount()))
            .ForMember(dest => dest.TotalPrice,
                opt => opt.MapFrom(src => src.GetTotalPrice()))
            .ForMember(dest => dest.FinalPrice,
                opt => opt.MapFrom(src => src.GetFinalPrice()));

        CreateMap<Cart, CartDto>()
            .ForMember(dest => dest.ItemCount,
                opt => opt.MapFrom(src => src.GetItemCount()))
            .ForMember(dest => dest.TotalAmount,
                opt => opt.MapFrom(src => src.GetTotalAmount()))
            .ForMember(dest => dest.FinalAmount,
                opt => opt.MapFrom(src => src.GetFinalAmount()))
            .ForMember(dest => dest.TotalDays,
                opt => opt.MapFrom(src => src.GetTotalDays()));

        // PROMOTION MAPPINGS
        CreateMap<Promotion, PromotionDto>()
            .ForMember(dest => dest.ApplicableBoothTypeIds,
                opt => opt.MapFrom(src => src.ApplicableBoothTypeIds.ToList()));
        CreateMap<CreatePromotionDto, Promotion>();
        CreateMap<UpdatePromotionDto, Promotion>();

        // HOMEPAGE CONTENT MAPPINGS
        CreateMap<HomePageSection, HomePageSectionDto>()
            .ForMember(dest => dest.IsValidForDisplay,
                opt => opt.MapFrom(src => src.IsValidForDisplay()));
        CreateMap<CreateHomePageSectionDto, HomePageSection>();
        CreateMap<UpdateHomePageSectionDto, HomePageSection>();

        // FILE MAPPINGS
        CreateMap<UploadedFile, UploadedFileDto>()
            .ForMember(dest => dest.ContentBase64, opt => opt.Ignore()); // Populated manually when needed

        // NOTIFICATION MAPPINGS
        CreateMap<UserNotification, NotificationDto>()
            .ForMember(dest => dest.Severity, opt => opt.MapFrom(src => MapNotificationSeverity(src.Severity)));

        // PAYMENT TRANSACTION MAPPINGS
        CreateMap<P24Transaction, PaymentTransactionDto>()
            .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src => src.Verified));
    }

    private static Application.Contracts.Notifications.NotificationSeverity MapNotificationSeverity(Domain.Notifications.NotificationSeverity severity)
    {
        return severity switch
        {
            Domain.Notifications.NotificationSeverity.Success => Application.Contracts.Notifications.NotificationSeverity.Success,
            Domain.Notifications.NotificationSeverity.Warning => Application.Contracts.Notifications.NotificationSeverity.Warning,
            Domain.Notifications.NotificationSeverity.Error => Application.Contracts.Notifications.NotificationSeverity.Error,
            _ => Application.Contracts.Notifications.NotificationSeverity.Info
        };
    }

    private static string GetBoothStatusDisplayName(BoothStatus status)
    {
        return status switch
        {
            BoothStatus.Available => "Dost�pne",
            BoothStatus.Rented => "Wynaj�te",
            BoothStatus.Maintenance => "Konserwacja",
            _ => status.ToString()
        };
    }

    private static string GetRentalStatusDisplayName(RentalStatus status)
    {
        return status switch
        {
            RentalStatus.Draft => "Projekt",
            RentalStatus.Active => "Aktywne",
            RentalStatus.Extended => "Przed�u�one",
            RentalStatus.Expired => "Wygas�e",
            RentalStatus.Cancelled => "Anulowane",
            _ => status.ToString()
        };
    }


    private static string GetCurrencyDisplayName(Currency currency)
    {
        return currency switch
        {
            Currency.PLN => "PLN",
            Currency.EUR => "EUR",
            Currency.USD => "USD",
            Currency.GBP => "GBP",
            Currency.CZK => "CZK",
            _ => currency.ToString()
        };
    }
}
