using Microsoft.EntityFrameworkCore;
using ProductService.DataAccess.Data;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.DataAccess.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ProductDbContext _context;

        public OrderRepository(ProductDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Order>> GetOrdersByUser(string userId)
        {
            return await _context.Orders
                .Include(o => o.Items) // Now works with Microsoft.EntityFrameworkCore
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync(); // Now works with Microsoft.EntityFrameworkCore
        }
    }
}
