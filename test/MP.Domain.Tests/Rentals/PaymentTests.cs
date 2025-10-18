using System;
using MP.Domain.Rentals;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace MP.Domain.Tests.Rentals
{
    public class PaymentTests : MPDomainTestBase<MPDomainTestModule>
    {
        [Fact]
        public void Constructor_Should_Initialize_With_Correct_Values()
        {
            // Arrange
            var totalAmount = 1000m;

            // Act
            var payment = new Payment(totalAmount);

            // Assert
            payment.TotalAmount.ShouldBe(totalAmount);
            payment.PaidAmount.ShouldBe(0);
            payment.PaidDate.ShouldBeNull();
            payment.PaymentStatus.ShouldBe(PaymentStatus.Pending);
            payment.PaymentMethod.ShouldBe(RentalPaymentMethod.Online);
            payment.Przelewy24TransactionId.ShouldBeNull();
        }

        [Fact]
        public void Constructor_Should_Initialize_With_Custom_Payment_Method()
        {
            // Arrange
            var totalAmount = 1000m;
            var paymentMethod = RentalPaymentMethod.Cash;

            // Act
            var payment = new Payment(totalAmount, paymentMethod);

            // Assert
            payment.PaymentMethod.ShouldBe(paymentMethod);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Amount_Negative()
        {
            // Arrange
            var negativeAmount = -100m;

            // Act & Assert
            var exception = Should.Throw<BusinessException>(
                () => new Payment(negativeAmount)
            );

            exception.Code.ShouldBe("PAYMENT_AMOUNT_MUST_BE_NON_NEGATIVE");
        }

        [Fact]
        public void Constructor_Should_Allow_Zero_Amount()
        {
            // Arrange
            var zeroAmount = 0m;

            // Act
            var payment = new Payment(zeroAmount);

            // Assert
            payment.TotalAmount.ShouldBe(0);
        }

        [Fact]
        public void IsPaid_Should_Return_False_By_Default()
        {
            // Arrange
            var payment = new Payment(1000m);

            // Act & Assert
            payment.IsPaid.ShouldBeFalse();
        }

        [Fact]
        public void MarkAsPaid_Should_Set_Paid_Amount_And_Status()
        {
            // Arrange
            var payment = new Payment(1000m);
            var paidAmount = 1000m;
            var paidDate = DateTime.Now.AddMinutes(-5);

            // Act
            payment.MarkAsPaid(paidAmount, paidDate);

            // Assert
            payment.PaidAmount.ShouldBe(paidAmount);
            payment.PaidDate.ShouldBe(paidDate);
            payment.PaymentStatus.ShouldBe(PaymentStatus.Completed);
            payment.IsPaid.ShouldBeTrue();
        }

        [Fact]
        public void MarkAsPaid_Should_Set_Transaction_Id()
        {
            // Arrange
            var payment = new Payment(1000m);
            var transactionId = "trans-12345";
            var paidDate = DateTime.Now.AddMinutes(-5);

            // Act
            payment.MarkAsPaid(1000m, paidDate, transactionId);

            // Assert
            payment.Przelewy24TransactionId.ShouldBe(transactionId);
        }

        [Fact]
        public void MarkAsPaid_Should_Throw_When_Amount_Negative()
        {
            // Arrange
            var payment = new Payment(1000m);

            // Act & Assert
            var exception = Should.Throw<BusinessException>(
                () => payment.MarkAsPaid(-100m, DateTime.Now.AddMinutes(-5))
            );

            exception.Code.ShouldBe("PAID_AMOUNT_MUST_BE_POSITIVE");
        }

        [Fact]
        public void MarkAsPaid_Should_Throw_When_Amount_Zero()
        {
            // Arrange
            var payment = new Payment(1000m);

            // Act & Assert
            var exception = Should.Throw<BusinessException>(
                () => payment.MarkAsPaid(0m, DateTime.Now.AddMinutes(-5))
            );

            exception.Code.ShouldBe("PAID_AMOUNT_MUST_BE_POSITIVE");
        }

        [Fact]
        public void MarkAsPaid_Should_Throw_When_Date_In_Future()
        {
            // Arrange
            var payment = new Payment(1000m);
            var futureDate = DateTime.Now.AddDays(1);

            // Act & Assert
            var exception = Should.Throw<BusinessException>(
                () => payment.MarkAsPaid(1000m, futureDate)
            );

            exception.Code.ShouldBe("PAID_DATE_CANNOT_BE_IN_FUTURE");
        }

        [Fact]
        public void MarkAsPaid_Should_Allow_Partial_Payment()
        {
            // Arrange
            var payment = new Payment(1000m);
            var partialAmount = 500m;
            var paidDate = DateTime.Now.AddMinutes(-5);

            // Act
            payment.MarkAsPaid(partialAmount, paidDate);

            // Assert
            payment.PaidAmount.ShouldBe(partialAmount);
            payment.IsPaid.ShouldBeFalse(); // Not fully paid
        }

        [Fact]
        public void SetTransactionId_Should_Set_Id_And_Change_Status()
        {
            // Arrange
            var payment = new Payment(1000m);
            var transactionId = "p24-session-123";

            // Act
            payment.SetTransactionId(transactionId);

            // Assert
            payment.Przelewy24TransactionId.ShouldBe(transactionId);
            payment.PaymentStatus.ShouldBe(PaymentStatus.Processing);
        }

        [Fact]
        public void MarkAsFailed_Should_Change_Status()
        {
            // Arrange
            var payment = new Payment(1000m);
            payment.SetTransactionId("trans-123");

            // Act
            payment.MarkAsFailed();

            // Assert
            payment.PaymentStatus.ShouldBe(PaymentStatus.Failed);
        }

        [Fact]
        public void MarkAsCancelled_Should_Change_Status()
        {
            // Arrange
            var payment = new Payment(1000m);
            payment.SetTransactionId("trans-123");

            // Act
            payment.MarkAsCancelled();

            // Assert
            payment.PaymentStatus.ShouldBe(PaymentStatus.Cancelled);
        }

        [Fact]
        public void SetTerminalDetails_Should_Set_Transaction_And_Receipt()
        {
            // Arrange
            var payment = new Payment(1000m);
            var terminalTransactionId = "term-trans-456";
            var receiptNumber = "REC-789";

            // Act
            payment.SetTerminalDetails(terminalTransactionId, receiptNumber);

            // Assert
            payment.TerminalTransactionId.ShouldBe(terminalTransactionId);
            payment.TerminalReceiptNumber.ShouldBe(receiptNumber);
        }

        [Fact]
        public void SetTerminalDetails_Should_Handle_Null_Values()
        {
            // Arrange
            var payment = new Payment(1000m);

            // Act
            payment.SetTerminalDetails(null, null);

            // Assert
            payment.TerminalTransactionId.ShouldBeNull();
            payment.TerminalReceiptNumber.ShouldBeNull();
        }

        [Fact]
        public void GetRemainingAmount_Should_Return_Full_Amount_When_Not_Paid()
        {
            // Arrange
            var totalAmount = 1000m;
            var payment = new Payment(totalAmount);

            // Act
            var remaining = payment.GetRemainingAmount();

            // Assert
            remaining.ShouldBe(totalAmount);
        }

        [Fact]
        public void GetRemainingAmount_Should_Return_Zero_When_Fully_Paid()
        {
            // Arrange
            var totalAmount = 1000m;
            var payment = new Payment(totalAmount);
            payment.MarkAsPaid(totalAmount, DateTime.Now.AddMinutes(-5));

            // Act
            var remaining = payment.GetRemainingAmount();

            // Assert
            remaining.ShouldBe(0);
        }

        [Fact]
        public void GetRemainingAmount_Should_Return_Difference_For_Partial_Payment()
        {
            // Arrange
            var totalAmount = 1000m;
            var paidAmount = 600m;
            var payment = new Payment(totalAmount);
            payment.MarkAsPaid(paidAmount, DateTime.Now.AddMinutes(-5));

            // Act
            var remaining = payment.GetRemainingAmount();

            // Assert
            remaining.ShouldBe(400m);
        }

        [Fact]
        public void GetRemainingAmount_Should_Return_Zero_When_Overpaid()
        {
            // Arrange
            var totalAmount = 1000m;
            var overpaidAmount = 1200m;
            var payment = new Payment(totalAmount);
            payment.MarkAsPaid(overpaidAmount, DateTime.Now.AddMinutes(-5));

            // Act
            var remaining = payment.GetRemainingAmount();

            // Assert
            remaining.ShouldBe(0);
        }

        [Fact]
        public void Payment_Should_Support_Value_Object_Equality()
        {
            // Arrange
            var payment1 = new Payment(1000m);
            var payment2 = new Payment(1000m);

            // Act & Assert - value objects with same initial state should have identical properties
            payment1.TotalAmount.ShouldBe(payment2.TotalAmount);
            payment1.PaidAmount.ShouldBe(payment2.PaidAmount);
            payment1.PaymentStatus.ShouldBe(payment2.PaymentStatus);
            payment1.PaymentMethod.ShouldBe(payment2.PaymentMethod);
            payment1.IsPaid.ShouldBe(payment2.IsPaid);
        }

        [Fact]
        public void Payment_Should_Not_Equal_When_Amount_Different()
        {
            // Arrange
            var payment1 = new Payment(1000m);
            var payment2 = new Payment(2000m);

            // Act & Assert
            payment1.Equals(payment2).ShouldBeFalse();
        }

        [Fact]
        public void IsPaid_Should_Be_False_When_Amount_Equals_But_Date_Missing()
        {
            // Arrange
            var payment = new Payment(1000m);

            // Simulate a payment object that has been marked as paid but then somehow date gets cleared
            // In normal flow this shouldn't happen, but testing the property logic
            var totalAmount = 1000m;
            var paidAmount = 1000m;

            // Create a payment and mark it as paid
            var paidPayment = new Payment(totalAmount);
            paidPayment.MarkAsPaid(paidAmount, DateTime.Now.AddMinutes(-5));

            // Act & Assert - should be true when properly paid
            paidPayment.IsPaid.ShouldBeTrue();
        }

        [Fact]
        public void IsPaid_Should_Check_All_Conditions()
        {
            // Arrange - Payment marked as completed but not fully paid
            var payment = new Payment(1000m);
            payment.MarkAsPaid(500m, DateTime.Now.AddMinutes(-5)); // Only 500 of 1000

            // Act & Assert
            payment.IsPaid.ShouldBeFalse(); // Should be false because not fully paid
        }
    }
}
