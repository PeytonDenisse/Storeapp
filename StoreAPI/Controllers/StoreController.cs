using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wkhtmltopdf.NetCore;

namespace StoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoreController : ControllerBase
    {
        private readonly IGeneratePdf _generatePdf;
        private readonly StoreDbContext _context;

        public StoreController(IGeneratePdf generatePdf, StoreDbContext context)
        {
            _generatePdf = generatePdf;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var store = await _context.Store.FirstOrDefaultAsync();
            var result = await _generatePdf.GetPdf("Templates/StoreTemplate.cshtml",
                "Saludos desde el controller");
            return result;
        }
    }
}
