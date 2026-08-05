using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class PaymentService:IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        public PaymentService(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }


        public async Task<IEnumerable<Payment>>GetAllAsync(Guid tenantId)
        {

            return await _paymentRepository.GetAllAsync(tenantId);
        }

        public async Task <Payment>GetByIdAsync(Guid Id)
        {
            return await _paymentRepository.GetByIdAsync(Id);
        }


        public async Task<Payment>CreateAsync(Payment payement)
        {
            if (payement.Amount < 0)
                throw new Exception("Invalide Amount");

            payement.PaymentDate = DateTime.UtcNow;
            payement.TransactionId = Guid.NewGuid().ToString();
            payement.PaymentStatus = "Success";
            return await _paymentRepository.AddAsync(payement);
        }

        public async  Task<bool>DeleteAsync(Guid Id)
        {
            var payment=await _paymentRepository.GetByIdAsync(Id);
            if (payment == null)
            {
                return false;
            }
            return await _paymentRepository.DeleteAsync(payment);
        }
    }
}
