using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Infrastructure.Repositories
{
    public class PaymentRepository:IPaymentRepository
    {
        private readonly ApplicationDbContext _context;
        public PaymentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Payment>> GetAllAsync(Guid tenantId)
        {
            return await _context.Payments.Where(p => p.TenantId == tenantId).ToListAsync();
        }




        public async Task<Payment> GetByIdAsync(Guid Id)
        {
            return await _context.Payments.FindAsync(Id);
        }

        public async Task<Payment> AddAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();
            return payment;


        }


        public async Task<bool> DeleteAsync(Payment payment)
        {

             _context.Remove(payment);
         var result=   await _context.SaveChangesAsync();
            return result > 0;
            
    } }
}
