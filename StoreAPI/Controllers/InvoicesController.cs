using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreApi.Models.DTOs;
using StoreApi.Models.Entities;

namespace StoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly StoreDbContext _context;

        public InvoicesController(StoreDbContext context)
        {
            _context = context;
        }

        // GET: api/invoices?orderId=1&isPaid=true
        [HttpGet]
        public async Task<ActionResult<List<Invoice>>> GetInvoices(
            [FromQuery] int? orderId,
            [FromQuery] bool? isPaid)
        {
            var query = _context.Invoice
                .Include(i => i.Orders)
                    .ThenInclude(o => o.SystemUser)
                .AsQueryable();

            if (orderId.HasValue)
                query = query.Where(i => i.Orders.Any(o => o.Id == orderId.Value));

            if (isPaid.HasValue)
                query = query.Where(i => i.IsPaid == isPaid.Value);

            var invoices = await query
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();

            return Ok(invoices);
        }

        // GET: api/invoices/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Invoice>> GetInvoice(int id)
        {
            var invoice = await _context.Invoice
                .Include(i => i.Orders)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();
            return Ok(invoice);
        }

        // POST: api/invoices
        [HttpPost]
        public async Task<ActionResult<Invoice>> CreateInvoice([FromBody] InvoiceDTO dto)
        {
            if (dto == null) return BadRequest("Body requerido.");
            if (dto.OrderIds == null || dto.OrderIds.Count == 0)
                return BadRequest("OrderIds es requerido.");
            if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
                return BadRequest("InvoiceNumber es requerido.");
            if (string.IsNullOrWhiteSpace(dto.Currency))
                return BadRequest("Currency es requerido.");
            if (string.IsNullOrWhiteSpace(dto.BillingName))
                return BadRequest("BillingName es requerido.");

            // Verificar órdenes
            var ids = dto.OrderIds.Distinct().ToList();
            var orders = await _context.Order.Where(o => ids.Contains(o.Id)).ToListAsync();
            var faltantes = ids.Except(orders.Select(o => o.Id)).ToList();
            if (faltantes.Any())
                return BadRequest($"OrderIds inexistentes: {string.Join(", ", faltantes)}");

            // Validar número único
            var duplicada = await _context.Invoice.AnyAsync(i => i.InvoiceNumber == dto.InvoiceNumber);
            if (duplicada)
                return Conflict($"InvoiceNumber '{dto.InvoiceNumber}' ya existe.");

            // Validaciones básicas
            if (dto.DueDate.HasValue && dto.DueDate < dto.IssueDate)
                return BadRequest("DueDate no puede ser menor a IssueDate.");
            if (dto.IsPaid && dto.PaymentDate is null)
                return BadRequest("Si IsPaid=true, PaymentDate es requerido.");

            // Calcular total
            var total = dto.Total ?? (dto.Subtotal + dto.Tax);

            var invoice = new Invoice
            {
                InvoiceNumber = dto.InvoiceNumber,
                IssueDate = dto.IssueDate,
                DueDate = dto.DueDate,
                Subtotal = dto.Subtotal,
                Tax = dto.Tax,
                Total = total,
                Currency = dto.Currency,
                IsPaid = dto.IsPaid,
                PaymentDate = dto.PaymentDate,
                BillingName = dto.BillingName,
                BillingAddress = dto.BillingAddress,
                BillingEmail = dto.BillingEmail,
                TaxId = dto.TaxId,
                Orders = orders
            };

            _context.Invoice.Add(invoice);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
        }
    }
}
