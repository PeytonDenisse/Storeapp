using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreApi.Models.DTOs;
using StoreApi.Models.Entities;
using OpenAI.Chat;


namespace StoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly StoreDbContext _context;
        private readonly IConfiguration _config;

        public InvoicesController(StoreDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
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
        
        // POST: api/invoices/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult> CreateInvoicesBulk([FromBody] List<InvoiceDTO> dtos)
        {
            if (dtos is null || dtos.Count == 0)
                return BadRequest("No se recibieron facturas.");

            // Validar duplicados en el payload (InvoiceNumber)
            var duplicadosEnEntrada = dtos
                .Where(d => !string.IsNullOrWhiteSpace(d.InvoiceNumber))
                .GroupBy(d => d.InvoiceNumber.Trim())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicadosEnEntrada.Any())
                return BadRequest($"Hay números de factura repetidos en la entrada: {string.Join(", ", duplicadosEnEntrada)}");

            // Juntar todos los OrderIds requeridos por el batch
            var allOrderIds = dtos
                .Where(d => d.OrderIds != null)
                .SelectMany(d => d.OrderIds)
                .Distinct()
                .ToList();

            // Traer órdenes de una sola vez
            var allOrders = await _context.Order
                .Where(o => allOrderIds.Contains(o.Id))
                .ToListAsync();

            var encontrados = allOrders.Select(o => o.Id).ToHashSet();
            var faltantes = allOrderIds.Where(id => !encontrados.Contains(id)).ToList();
            if (faltantes.Any())
                return BadRequest($"OrderIds inexistentes: {string.Join(", ", faltantes)}");

            // Validar que no existan InvoiceNumber ya tomados en DB
            var invoiceNumbers = dtos
                .Where(d => !string.IsNullOrWhiteSpace(d.InvoiceNumber))
                .Select(d => d.InvoiceNumber.Trim())
                .Distinct()
                .ToList();

            var yaExisten = await _context.Invoice
                .Where(i => invoiceNumbers.Contains(i.InvoiceNumber))
                .Select(i => i.InvoiceNumber)
                .ToListAsync();

            if (yaExisten.Any())
                return Conflict($"InvoiceNumber ya existentes en base de datos: {string.Join(", ", yaExisten)}");

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var newInvoices = new List<Invoice>();

                foreach (var dto in dtos)
                {
                    // Validaciones por item
                    if (dto == null) throw new ArgumentException("Hay un elemento nulo en la lista.");
                    if (dto.OrderIds == null || dto.OrderIds.Count == 0)
                        throw new ArgumentException($"OrderIds es requerido para la factura '{dto.InvoiceNumber ?? "(sin número)"}'.");
                    if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
                        throw new ArgumentException("InvoiceNumber es requerido.");
                    if (string.IsNullOrWhiteSpace(dto.Currency))
                        throw new ArgumentException($"Currency es requerido para la factura '{dto.InvoiceNumber}'.");
                    if (string.IsNullOrWhiteSpace(dto.BillingName))
                        throw new ArgumentException($"BillingName es requerido para la factura '{dto.InvoiceNumber}'.");

                    if (dto.DueDate.HasValue && dto.DueDate < dto.IssueDate)
                        throw new ArgumentException($"DueDate no puede ser menor a IssueDate en '{dto.InvoiceNumber}'.");
                    if (dto.IsPaid && dto.PaymentDate is null)
                        throw new ArgumentException($"Si IsPaid=true, PaymentDate es requerido en '{dto.InvoiceNumber}'.");

                    // Resolver órdenes para esta factura
                    var ids = dto.OrderIds.Distinct().ToList();
                    var ordersForInvoice = allOrders.Where(o => ids.Contains(o.Id)).ToList();

                    // Calcular total si viene nulo
                    var total = dto.Total ?? (dto.Subtotal + dto.Tax);

                    var invoice = new Invoice
                    {
                        InvoiceNumber = dto.InvoiceNumber.Trim(),
                        IssueDate = dto.IssueDate,
                        DueDate = dto.DueDate,
                        Subtotal = dto.Subtotal,
                        Tax = dto.Tax,
                        Total = total,
                        Currency = dto.Currency.Trim(),
                        IsPaid = dto.IsPaid,
                        PaymentDate = dto.PaymentDate,
                        BillingName = dto.BillingName,
                        BillingAddress = dto.BillingAddress,
                        BillingEmail = dto.BillingEmail,
                        TaxId = dto.TaxId,
                        Orders = ordersForInvoice
                    };

                    newInvoices.Add(invoice);
                }

                _context.Invoice.AddRange(newInvoices);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                // Puedes devolver solo un resumen para no saturar el payload
                return Ok(new
                {
                    message = "Facturas agregadas",
                    count = newInvoices.Count,
                    ids = newInvoices.Select(i => i.Id).ToList()
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return BadRequest(ex.Message);
            }
        }

        
        
        
        
        // GET: api/invoices/ai-analyze
        [HttpGet("ai-analyze")]
        public async Task<IActionResult> AnalyzeInvoices()
        {
            var openAIKey = _config["OpenAIKey"];
            if (string.IsNullOrWhiteSpace(openAIKey))
                return StatusCode(500, new { error = "Falta configurar OpenAIKey." });

            
            var invoices = await _context.Invoice
                .AsNoTracking()
                .ToListAsync();

            
            var summary = invoices.Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.IssueDate,
                i.DueDate,
                i.Subtotal,
                i.Tax,
                i.Total,
                i.Currency,
                i.IsPaid,
                i.PaymentDate
            });

            var jsonData = JsonSerializer.Serialize(summary);

            var client = new ChatClient(
                model: "gpt-5-mini",
                apiKey: openAIKey
            );

           
            var prompt = Prompts.GenerateInvoicesPrompt(jsonData);

            var result = await client.CompleteChatAsync(new[]
            {
                new UserChatMessage(prompt)
            });

            var aiText = result.Value.Content[0].Text?.Trim() ?? "error";

            
            try
            {
                using var doc = JsonDocument.Parse(aiText);
                var root = doc.RootElement;

                string[] required =
                {
                    "totalInvoices", "paidInvoices", "unpaidInvoices",
                    "totalRevenue", "averageInvoiceAmount",
                    "commonCurrencies", "patterns"
                };

                foreach (var key in required)
                {
                    if (!root.TryGetProperty(key, out _))
                        return Ok(new { error = aiText });
                }

                return new ContentResult
                {
                    Content = aiText,
                    ContentType = "application/json",
                    StatusCode = 200
                };
            }
            catch
            {
                return Ok(new { error = aiText });
            }
        }

        
        
    }
}
