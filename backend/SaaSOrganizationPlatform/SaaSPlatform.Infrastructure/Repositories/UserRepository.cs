using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Infrastructure.Data;
using SaaSPlatform_Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Infrastructure.Repositories
{
    public class UserRepository:IUserRepository
    {

        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context =context;
        }

        public async Task<IEnumerable<User>> GetAllUsers(Guid tenantId)
        {
            return await _context.Users.Where(u=>u.TenantId==tenantId).ToListAsync();
        }



        public async Task<User?> GetUserById(Guid Id)
        {
            return await _context.Users.FindAsync(Id);
        }


       


        public async Task<User> CreateUser(User user)
        {
             _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }


        public async Task<bool> UpdateUser ( Guid Id,User user)
        {
            var existingUser = await _context.Users.FindAsync(Id);
            if (existingUser == null)
                return false;
            
            if (!ReferenceEquals(existingUser, user))
            {
                _context.Entry(existingUser).CurrentValues.SetValues(user);
            }

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> DeleteUser(Guid Id)
        {
            var user = await _context.Users.FindAsync(Id);
            if (user == null)
             return false;

            _context.Users.Remove(user);

            await _context.SaveChangesAsync();
            return true;

        }

        public async Task<User?> GetByEmailAsync(string Email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);

        }
    }
      
   
    
}
