using CLDV6212_PART_3.Data;
using CLDV6212_PART_3.Models;
using CLDV6212_PART_3.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLDV6212_PART_3.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly QueueService _queueService;

     

        public OrdersController(ApplicationDBContext context, UserManager<IdentityUser> userManager, QueueService queueService)
        {
            _context = context;
            _userManager = userManager;
            _queueService = queueService;
        }


        // Admin View
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.Status != "Shopping" && o.TotalPrice.HasValue)
                .ToListAsync();

            var orderViewModels = orders.Select(o => new OrderAdminViewModel
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                UserEmail = o.User.Email,
                Status = o.Status,
                TotalPrice = (decimal)o.TotalPrice
            }).ToList();

            return View(orderViewModels);
        }

        // Process Order
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ProcessOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            // Update the OrderStatus to "Approved"
            order.Status = "Approved";

            // Send a message to the queue
            string message = $"Processing Order: Order ID: {order.OrderId}";
            await _queueService.SendMessageAsync("createdorders", message);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Admin));
        }


        [Authorize(Roles = "Client,Admin")]
        public async Task<IActionResult> OrderHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = await _userManager.GetUserIdAsync(user);

            var orders = await _context.Orders
                .Where(o => o.UserId == userId && o.Status != "Shopping" && o.TotalPrice.HasValue)
                .Select(o => new OrderHistoryViewModel
                {
                    OrderId = o.OrderId,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    TotalPrice = (decimal)o.TotalPrice
                })
                .ToListAsync();

            return View(orders);
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }
}
