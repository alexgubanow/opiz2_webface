using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace main_app.Pages
{
    public class SettingsModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public SettingsModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
    }
}
