using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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
    public class SettingsModel : ModelExpand
    {
        private readonly ILogger<IndexModel> _logger;
        public List<string> SSIDList { get; set; }
        [BindProperty]
        public WiFiCreds WiFiCreds { get; set; }
        public SettingsModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
            SSIDList = new List<string>();
            WiFiCreds = new WiFiCreds();
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
                        var args = "dev wifi connect " + WiFiCreds.SSID;
                        if (WiFiCreds.Password != null && WiFiCreds.Password.Length > 0)
                        {
                            args += " password " + WiFiCreds.Password;
                        }
                        int status = await RunProcessAsync("nmcli", args, ref output);
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
            int status = -1;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                status = await RunProcessAsync(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe", "netsh wlan show network  mode=bssid", ref output);
                var matchList = Regex.Matches(output, @"(?<=^SSID...\: ).*(?=\s)", RegexOptions.Multiline);
                SSIDList = matchList.Cast<Match>().Select(match => match.Value).ToList();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                status = await RunProcessAsync(@"nmcli", "-f SSID dev wifi", ref output);
                //status = await RunProcessAsync("ls", "-la", ref output);
                _logger.LogError("nmcli out: [{output}]", output);
                if (output.Length > 0)
                {
                    SSIDList.AddRange(output.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Skip(1));
                }
            }
            else
            {
                SSIDList.Add("unknown arch");
                return -1;
            }
            return status;
        }
        Task<int> RunProcessAsync(string fileName, string args, ref string output)
        {
            var tcs = new TaskCompletionSource<int>();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = fileName,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };
            process.Start();
            string error = process.StandardError.ReadToEnd();
            //output = process.StandardOutput.ReadToEnd();
            //process.WaitForExit();

            var tmpString = new StringBuilder();
            while (!process.HasExited)
            {
                tmpString.Append(process.StandardOutput.ReadToEnd());
            }
            output = tmpString.ToString();

            if (error != null && error.Length > 0)
            {
                _logger.LogError("app [{fileName} {args}] rised error {output}", fileName, args, error);
            }
            tcs.SetResult(process.ExitCode);
            process.Dispose();
            return tcs.Task;
        }
    }
}
