using SaaSPlatform.Application.DTOS.Payments;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class PaymentService:IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ISystemLogRepository _systemLogs;
        private readonly ITenantRepository? _tenantRepository;
        private readonly ISubscriptionPlanRepository? _planRepository;

        public PaymentService(
            IPaymentRepository paymentRepository, 
            ISystemLogRepository systemLogs,
            ITenantRepository? tenantRepository = null,
            ISubscriptionPlanRepository? planRepository = null)
        {
            _paymentRepository = paymentRepository;
            _systemLogs = systemLogs;
            _tenantRepository = tenantRepository;
            _planRepository = planRepository;
        }

        public async Task<IEnumerable<Payment>>GetAllAsync(Guid tenantId)
        {
            return await _paymentRepository.GetAllAsync(tenantId);
        }

        public async Task<IReadOnlyList<AdminTransactionDto>> GetAdminTransactionsAsync()
        {
            return await _paymentRepository.GetAdminTransactionsAsync();
        }

        public async Task <Payment>GetByIdAsync(Guid Id)
        {
            return await _paymentRepository.GetByIdAsync(Id);
        }


        public async Task<Payment>CreateAsync(Payment payement, Guid? userId = null)
        {
            if (payement.Amount < 0)
                throw new Exception("Invalide Amount");

            payement.PaymentDate = DateTime.UtcNow;
            payement.TransactionId = Guid.NewGuid().ToString();
            payement.PaymentStatus = "Success";
            var created = await _paymentRepository.AddAsync(payement);

            // The acting user comes from the JWT via the controller; fall back to the payer on the payment.
            Guid? actorId = userId;
            if (!actorId.HasValue && created.UserId != Guid.Empty)
            {
                actorId = created.UserId;
            }

            // Audit trail only: amount, method, status and transaction id. No card or credential data is logged.
            await _systemLogs.LogAsync(
                "PAYMENT_RECEIVED",
                $"Payment of {created.Amount.ToString(CultureInfo.InvariantCulture)} via {created.PaymentMethod} recorded with status {created.PaymentStatus} (transaction {created.TransactionId}).",
                actorId, created.TenantId);

            // If this payment is for a subscription plan, update the tenant's plan
            if (_tenantRepository != null && payement.SubscriptionPlanId != Guid.Empty)
            {
                var tenant = await _tenantRepository.GetByIdAsync(payement.TenantId);
                if (tenant != null && !tenant.IsDeleted)
                {
                    tenant.SubscriptionPlanId = payement.SubscriptionPlanId;
                    await _tenantRepository.UpdateAsync(tenant);

                    string planName = "new";
                    if (_planRepository != null)
                    {
                        var plan = await _planRepository.GetByIdAsync(payement.SubscriptionPlanId);
                        if (plan != null) planName = plan.Name;
                    }

                    await _systemLogs.LogAsync(
                        "PLAN_UPGRADED",
                        $"Tenant {tenant.Name} upgraded to {planName} plan via payment {created.TransactionId}.",
                        actorId, tenant.Id);
                }
            }

            return created;
        }

        public async Task<bool>DeleteAsync(Guid Id, Guid? userId = null)
        {
            var payment=await _paymentRepository.GetByIdAsync(Id);
            if (payment == null)
            {
                return false;
            }

            var result = await _paymentRepository.DeleteAsync(payment);
            if (result)
            {
                await _systemLogs.LogAsync(
                    "PAYMENT_DELETED",
                    $"Payment {payment.TransactionId} of {payment.Amount.ToString(CultureInfo.InvariantCulture)} deleted.",
                    userId, payment.TenantId);
            }
            return result;
        }
    }
}
