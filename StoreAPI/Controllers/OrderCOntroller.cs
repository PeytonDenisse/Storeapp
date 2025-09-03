using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreApi.Models.DTOs;
using StoreApi.Models.Entities;

namespace StoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderCOntroller : ControllerBase
    
    {
        private readonly StoreDbContext _context;

        public OrderCOntroller(StoreDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Order>>> GetOrders()
        {
            var orders = await _context.Order
                .Include(o => o.SystemUser)
                .ToListAsync();
            return Ok(orders);
        }

        [HttpPost]
        public async Task<ActionResult> CreateOrder([FromBody] OrderCDTO order)
        {
            var newOrder = new Order()
            {
                SystemUserId = order.SystemUserId,
                CreatedAt = DateTime.Now,
                Total = order.Total,
            };

            _context.Order.Add(newOrder);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetOrders), new { id = newOrder.Id }, newOrder);
        }
    }
}
