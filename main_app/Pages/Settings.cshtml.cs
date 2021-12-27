using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace main_app.Pages
{
    public class WiFiCreds
    {
        [Required(ErrorMessage = "Required.")]
        [Display(Name = "SSID")]
        public string SSID { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
    }
    public class SettingsModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        public List<string> SSIDList { get; set; }
        [BindProperty]
        public WiFiCreds WiFiCreds { get; set; }
        public string Message { get; set; }
        public SettingsModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }
        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                await UpdateSSIDList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                throw;
            }
            return Page();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (WiFiCreds.SSID != null && WiFiCreds.SSID.Length > 0)
                    {
                        string output = "";
                        int status = await RunProcessAsync(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe", "netsh wlan show network  mode=bssid", ref output);
                        if (status == 0)
                        {
                            Message = "Connected to " + WiFiCreds.SSID;
                            _logger.LogTrace(Message + " output: " + output);
                        }
                        else
                        {
                            Message = "Error while connecting to " + WiFiCreds.SSID;
                            _logger.LogError(Message + " output: " + output);
                        }
                    }
                    await UpdateSSIDList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message, ex);
                    throw;
                }
            }
            return Page();
        }

        private async Task<int> UpdateSSIDList()
        {
            string output = "";
            int status = await RunProcessAsync(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe", "netsh wlan show network  mode=bssid", ref output);
            var matchList = Regex.Matches(output, @"(?<=^SSID...\: ).*(?=\s)", RegexOptions.Multiline);
            SSIDList = matchList.Cast<Match>().Select(match => match.Value).ToList();
            return status;
        }

        static Task<int> RunProcessAsync(string fileName, string args, ref string output)
        {
            var tcs = new TaskCompletionSource<int>();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    //FileName = $"/bin/bash",
                    FileName = fileName,
                    //WorkingDirectory = "/mnt",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };
            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };
            process.Start();

            string error = process.StandardError.ReadToEnd();
            output = process.StandardOutput.ReadToEnd();
            return tcs.Task;
        }
    }
}
