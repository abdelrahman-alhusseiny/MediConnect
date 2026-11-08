using MedicalSystem.DataAccess.Data;
using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.DataAccess.Repository
{
    public class PaymentRepository : Repository<Payment>, IPaymentRepository
    {
        private readonly ApplicationDbContext _db;
        public PaymentRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<PaymentResult> ProcessCardPaymentAsync(
            int appointmentId,
            decimal amount,
            string cardNumber,
            int expiryMonth,
            int expiryYear,
            string cvv)
        {
            try
            {
                string testToken = "tok_visa"; 

                var chargeOptions = new ChargeCreateOptions
                {
                    Amount = (long)(amount * 100), 
                    Currency = "usd",
                    Description = $"[TEST MODE] Appointment {appointmentId} payment",
                    Source = testToken
                };

                var chargeService = new ChargeService();
                var charge = await chargeService.CreateAsync(chargeOptions);

                return new PaymentResult
                {
                    Success = charge.Status == "succeeded",
                    TransactionId = charge.Id,
                    ErrorMessage = charge.FailureMessage
                };
            }
            catch (StripeException ex)
            {
                return new PaymentResult
                {
                    Success = false,
                    TransactionId = null,
                    ErrorMessage = $"Stripe test error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new PaymentResult
                {
                    Success = false,
                    TransactionId = null,
                    ErrorMessage = $"Unexpected test error: {ex.Message}"
                };
            }
        }

        public async Task UpdateAsync(Payment payment)
        {
            var objFromDb = await _db.Payments.FirstOrDefaultAsync(
                c => c.Id == payment.Id);

            if (objFromDb != null)
            {
                objFromDb.Amount = payment.Amount;
                objFromDb.Method = payment.Method;
                objFromDb.TransactionId = payment.TransactionId;
                objFromDb.Status = payment.Status;
            }
        }
    }
}
