using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;

namespace main_app.Pages
{
    public class ModelExpand : PageModel
    {
        public string Hostname { get; set; }
        public string Message { get; set; }
        public ModelExpand()
        {
            Hostname = Dns.GetHostName();
        }
    }
}
