using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using StoreApi.Models.DTOs;
using StoreApi.Models.Entities;
using System.Text.Json;


namespace StoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderCOntroller : ControllerBase

    {
        private readonly StoreDbContext _context;
        private readonly IConfiguration _config;

        public OrderCOntroller(
            StoreDbContext context,
            IConfiguration config
        )
        {
            _context = context;
            _config = config;
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

        [HttpPost("bulk")]
        public async Task<ActionResult> CreateOrderBulk([FromBody] List<OrderCDTO> orders)
        {
            if (orders == null || orders.Count == 0)
            {
                return BadRequest("no se recibieron ordenes");
            }
            //si yo voy a modificar varias tablas o si muevo muchos registros debo realizar una transaccion en sql

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // //FORMA DE USAR NORMAL 
                // //convertir lista de order DTO en lista de ordenes -> Porque ? por que el BDcontex necesita la entidad de Order no de ORderDTO
                //
                // var newOrders = new List<Order>();
                // foreach (var orderDTO in orders)
                // {
                //     var newOrder = new Order();
                //     newOrder.SystemUserId = orderDTO.SystemUserId;
                //     newOrder.Total = orderDTO.Total;
                //     newOrder.CreatedAt = DateTime.Now;
                //     newOrder.Total = orderDTO.Total;
                //     
                // }

                //USANFO linq

                var newOrders = orders
                    .Select(o => new Order()
                        {
                            SystemUserId = o.SystemUserId,
                            CreatedAt = DateTime.Now,
                            Total = o.Total,
                            OrderProducts = o.Products
                                .Select(op => new OrderProduct() { Amount = 1, ProductId = op })
                                .ToList()
                        }
                    )
                    .ToList();
                _context.Order.AddRange(newOrders);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok("Ordenes agregadas");
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return BadRequest(e.Message);
            }

        }

        [HttpGet("ai-analyze")]

        public async Task<ActionResult> AnalyzeOrder()
        {
            //Obtener APIKEY
            var openIAKEY = _config["OpenAIKey"];

            var client = new ChatClient(
                model: "gpt-5-mini",
                apiKey: openIAKEY
            );
            

            //primero se obtienen los datos 
            var orders = await _context.Order
                .Include(o => o.OrderProducts)
                .ThenInclude(o => o.Product)
                .ThenInclude(p => p.Store)
                .ToListAsync();
            var summary = orders.Select(o => new
            {
                o.Id,
                o.Total,
                o.CreatedAt,
                Products = o.OrderProducts.Select(op => new
                {
                    op.Product.Nombre,
                    op.Product.Price,
                    op.Product.Store.Description
                })
            });
            var jsonData = JsonSerializer.Serialize(summary);

            
            // todas las ordenes, con sus productos, con sus tiendas 
             var prompt = Prompts.GenerateOrdersPrompt(jsonData);
             var result = await client.CompleteChatAsync([
                 new UserChatMessage(prompt)
             ]);
            

            // se hace el promt

            // la ia analiza los datos y me responde 

            // se da una respuesta con los datos de la ia 
            var response = result.Value.Content[0].Text;
            //
            return Ok(response);
         }

    }

}
    

