using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
            try
            {
                var fileStream = new FileStream(Environment.CurrentDirectory + "/wifilist.txt", FileMode.Open);
                using var reader = new StreamReader(fileStream);
                string s = string.Empty;
                while ((s = reader.ReadLine()) != null)
                {
                    if (s != null && s != "")
                    {
                        SSIDList.Add(s);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("fail to open {}", Environment.CurrentDirectory + "wifilist.txt");
                _logger.LogError("Exception : {ex}\nMessage:{ex.Message}", ex, ex.Message);
            }
        }
        public async Task<IActionResult> OnGetAsync()
        {
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
                        int status = await Utils.RunProcessAsync("nmcli", "c down hotspot", _logger, ref output);
                        if (status == 0)
                        {
                            _logger.LogTrace("turn off hotspot");
                        }
                        else
                        {
                            Message = "Error while turn off hotspot";
                            _logger.LogError("{}", Message);
                        }
                        var args = "dev wifi connect " + WiFiCreds.SSID;
                        if (WiFiCreds.Password != null && WiFiCreds.Password.Length > 0)
                        {
                            args += " password " + WiFiCreds.Password;
                        }
                        await Task.Delay(3000);
                        output = "";
                        status = await Utils.RunProcessAsync("nmcli", args, _logger, ref output);
                        if (status == 0)
                        {
                            _logger.LogTrace("connected to {WiFiCreds.SSID}", WiFiCreds.SSID);
                        }
                        else
                        {
                            Message = "Error while connecting to " + WiFiCreds.SSID;
                            _logger.LogWarning("Error while connecting to {WiFiCreds.SSID}", WiFiCreds.SSID);
                            status = await Utils.RunProcessAsync(@"nmcli", "-t -f SSID dev wifi", _logger, ref output);
                            System.IO.File.WriteAllText(Environment.CurrentDirectory + "/wifilist.txt", output);
                            await Task.Delay(1000);
                            status = await Utils.RunProcessAsync("nmcli", "c up hotspot", _logger, ref output);
                            if (status == 0)
                            {
                                _logger.LogTrace("turn on hotspot");
                            }
                            else
                            {
                                Message = "Error while turn on hotspot";
                                _logger.LogError("{}", Message);
                            }
                        }
                    }
                    //await UpdateSSIDList();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception : {ex}\nMessage:{ex.Message}", ex, ex.Message);
                    throw;
                }
            }
            return Page();
        }
        //private async Task<int> UpdateSSIDList()
        //{
        //    string output = "";
        //    int status = -1;
        //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //    {
        //        status = await RunProcessAsync(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe", "netsh wlan show network  mode=bssid", ref output);
        //        var matchList = Regex.Matches(output, @"(?<=^SSID...\: ).*(?=\s)", RegexOptions.Multiline);
        //        SSIDList = matchList.Cast<Match>().Select(match => match.Value).ToList();
        //    }
        //    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        //    {
        //        status = await RunProcessAsync(@"nmcli", "-f SSID dev wifi", ref output);
        //        _logger.LogError("nmcli out: [{output}]", output);
        //        if (output.Length > 0)
        //        {
        //            SSIDList.AddRange(output.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Skip(1));
        //            SSIDList.RemoveAll(ssid => ssid == "--");
        //        }
        //    }
        //    else
        //    {
        //        SSIDList.Add("unknown arch");
        //        return -1;
        //    }
        //    return status;
        //}
    }
}
