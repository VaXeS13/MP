# Localization Keys for Calendar Components

These localization keys should be added to the backend resource files (*.json) in the `Localization` folder.

**IMPORTANT NOTE**:
- Short messages use localization keys with fallback defaults
- Long messages with dynamic values (dates, numbers) are generated directly in TypeScript code
- Only titles and short messages need to be added to backend localization files

## Calendar Validation Messages

### Day Selection (Short Messages - Use Localization)
- **MP::DayUnavailable**: "Day Unavailable"
- **MP::InvalidSelection**: "Invalid Selection"
- **MP::MinimumRentalPeriod**: "Minimum rental period is 7 days"
- **MP::Error**: "Error"
- **MP::FailedToLoadBoothData**: "Failed to load booth data"

### Gap Validation Titles (Use Localization)
- **MP::UnusableGapBeforeRental**: "Unusable Gap Before Rental"
- **MP::UnusableGapAfterRental**: "Unusable Gap After Rental"

### Gap Validation Details (Generated in Code - NO Localization Keys)
The detailed error messages with dates and gap days are generated directly in TypeScript:
- `"Your rental would leave a ${gapDays}-day gap before another rental..."`
- `"Your rental would leave a ${gapDays}-day gap after another rental..."`

These are NOT using localization keys because they contain dynamic date formatting and interpolated values.

## Example JSON Structure (English)

```json
{
  "culture": "en",
  "texts": {
    "MP::DayUnavailable": "Day Unavailable",
    "MP::InvalidSelection": "Invalid Selection",
    "MP::MinimumRentalPeriod": "Minimum rental period is 7 days",
    "MP::UnusableGapBeforeRental": "Unusable Gap Before Rental",
    "MP::UnusableGapAfterRental": "Unusable Gap After Rental",
    "MP::Error": "Error",
    "MP::FailedToLoadBoothData": "Failed to load booth data"
  }
}
```

## Polish Translation Example

```json
{
  "culture": "pl",
  "texts": {
    "MP::DayUnavailable": "Dzień niedostępny",
    "MP::InvalidSelection": "Nieprawidłowy wybór",
    "MP::MinimumRentalPeriod": "Minimalny okres wynajmu to 7 dni",
    "MP::UnusableGapBeforeRental": "Niedozwolona luka przed rezerwacją",
    "MP::UnusableGapAfterRental": "Niedozwolona luka po rezerwacji",
    "MP::Error": "Błąd",
    "MP::FailedToLoadBoothData": "Nie udało się załadować danych stoiska"
  }
}
```

## Note on Implementation

The application will work correctly **without** these localization keys in the backend:
- All messages have English fallback values in the code
- Adding these keys to backend localization files will enable multi-language support
- Detailed error messages with dates/numbers are always in English (generated in code)

## Backend Integration

Add these keys to:
- `src/MP.Domain.Shared/Localization/MP/en.json`
- `src/MP.Domain.Shared/Localization/MP/pl.json` (or other language files)

The LocalizationService will automatically use these translations when available, falling back to the English text provided in the code if the key is not found.
