-- Enable Stripe Payment Provider
-- This script enables Stripe and configures all necessary settings

USE MP;
GO

-- Enable Stripe Provider
IF EXISTS (SELECT 1 FROM AbpSettings WHERE [Name] = 'MP.PaymentProviders.Stripe.Enabled')
BEGIN
    UPDATE AbpSettings
    SET [Value] = 'True'
    WHERE [Name] = 'MP.PaymentProviders.Stripe.Enabled';
    PRINT '✅ Stripe Provider ENABLED (updated existing setting)';
END
ELSE
BEGIN
    INSERT INTO AbpSettings ([Id], [Name], [Value])
    VALUES (NEWID(), 'MP.PaymentProviders.Stripe.Enabled', 'True');
    PRINT '✅ Stripe Provider ENABLED (created new setting)';
END
GO

-- Set Publishable Key (if not exists)
IF NOT EXISTS (SELECT 1 FROM AbpSettings WHERE [Name] = 'MP.PaymentProviders.Stripe.PublishableKey')
BEGIN
    INSERT INTO AbpSettings ([Id], [Name], [Value])
    VALUES (NEWID(), 'MP.PaymentProviders.Stripe.PublishableKey', 'pk_test_51SEbgBQihiXumQfXroAsiten17Fq45ismKEFprs9xtHfcNvtece3fsj5e7IsKSSysvFhMHg2YT5LHP6UeQs5nud6003qa4dfdb');
    PRINT '✅ Stripe Publishable Key SET';
END
ELSE
BEGIN
    PRINT 'ℹ️  Stripe Publishable Key already exists';
END
GO

-- Set Secret Key (if not exists)
IF NOT EXISTS (SELECT 1 FROM AbpSettings WHERE [Name] = 'MP.PaymentProviders.Stripe.SecretKey')
BEGIN
    INSERT INTO AbpSettings ([Id], [Name], [Value])
    VALUES (NEWID(), 'MP.PaymentProviders.Stripe.SecretKey', 'sk_test_51SEbgBQihiXumQfXiJxoinXPhzMtGPtfOC7zHtKwhCsHjnIACnauSHczuaFQ3yjRX583ynGFN6XW4IhWfkwxmbBQ00MlgoLWQo');
    PRINT '✅ Stripe Secret Key SET';
END
ELSE
BEGIN
    PRINT 'ℹ️  Stripe Secret Key already exists';
END
GO

-- Set Webhook Secret
IF EXISTS (SELECT 1 FROM AbpSettings WHERE [Name] = 'MP.PaymentProviders.Stripe.WebhookSecret')
BEGIN
    UPDATE AbpSettings
    SET [Value] = 'whsec_1d761b19f163f0fa5663c1139a4765ae7f1bc3ca5af260906e8a59371bf02282'
    WHERE [Name] = 'MP.PaymentProviders.Stripe.WebhookSecret';
    PRINT '✅ Stripe Webhook Secret UPDATED';
END
ELSE
BEGIN
    INSERT INTO AbpSettings ([Id], [Name], [Value])
    VALUES (NEWID(), 'MP.PaymentProviders.Stripe.WebhookSecret', 'whsec_1d761b19f163f0fa5663c1139a4765ae7f1bc3ca5af260906e8a59371bf02282');
    PRINT '✅ Stripe Webhook Secret SET';
END
GO

-- Verify settings
SELECT [Name], [Value], [TenantId]
FROM AbpSettings
WHERE [Name] LIKE 'MP.PaymentProviders.Stripe.%'
ORDER BY [Name];
GO

PRINT '';
PRINT '=================================================';
PRINT '✅ STRIPE CONFIGURATION COMPLETE!';
PRINT '=================================================';
PRINT '';
PRINT 'Stripe Provider: ENABLED';
PRINT 'Webhook Forwarding: RUNNING (keep Stripe CLI running)';
PRINT 'Next Steps:';
PRINT '  1. Start backend API: dotnet run --project src/MP.HttpApi.Host';
PRINT '  2. Start frontend: cd angular && ng serve';
PRINT '  3. Test payment flow';
PRINT '';
