using MedicalSystem.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.DataAccess.Repository.IRepository
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        Task<PaymentResult> ProcessCardPaymentAsync(
                    int appointmentId,
                    decimal amount,
                    string cardNumber,
                    int expiryMonth,
                    int expiryYear,
                    string cvv);

        Task UpdateAsync(Payment payment);
    }
}
