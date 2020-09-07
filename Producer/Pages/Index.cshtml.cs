using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Producer.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IOrderEventsPublisher _orderEventsPublisher;

        public int OrderId { get; set; }
        public string EventDescription { get; set; }
        public IndexModel(ILogger<IndexModel> logger, IOrderEventsPublisher orderEventsPublisher)
        {
            _logger = logger;
            _orderEventsPublisher = orderEventsPublisher;
        }

        [BindProperty]
        public OrderEventModel OrderEvent { get; set; }

        public void OnGet()
        {

        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            _orderEventsPublisher.Publish(OrderEvent.OrderId, OrderEvent.Description);
            return Page();
        }
    }

    public class OrderEventModel
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public string Description { get; set; }
    }
}
