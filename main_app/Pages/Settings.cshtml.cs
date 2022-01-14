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
        //[Required(ErrorMessage = "Required.")]
        [Display(Name = "SSID")]
        public string SSID { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
    }
    public class UserCreds
    {
        //[Required(ErrorMessage = "Required.")]
        [Display(Name = "Userr")]
        public string User { get; set; }

        [DataType(DataType.Password)]
        //[Required(ErrorMessage = "Required.")]
        [Display(Name = "Password")]
        public string Password { get; set; }
    }
    public class SettingsModel : ModelExpand
    {
        private readonly ILogger<IndexModel> _logger;
        public List<string> SSIDList { get; set; }
        [BindProperty]
        public WiFiCreds WifiCreds { get; set; }
        [BindProperty]
        public UserCreds UserCreds { get; set; }
        public SettingsModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
            SSIDList = new List<string>();
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
        public void OnGet()
        {
        }
        public async Task<IActionResult> OnPostWiFIConnectAsync()
        {
            if (!ModelState.IsValid) { return Page(); }
            try
            {
                if (WifiCreds.SSID != null && WifiCreds.SSID.Length > 0)
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
                    var args = "dev wifi connect " + WifiCreds.SSID;
                    if (WifiCreds.Password != null && WifiCreds.Password.Length > 0)
                    {
                        args += " password " + WifiCreds.Password;
                    }
                    await Task.Delay(3000);
                    output = "";
                    status = await Utils.RunProcessAsync("nmcli", args, _logger, ref output);
                    if (status == 0)
                    {
                        _logger.LogTrace("connected to {WiFiCreds.SSID}", WifiCreds.SSID);
                    }
                    else
                    {
                        Message = "Error while connecting to " + WifiCreds.SSID;
                        _logger.LogWarning("Error while connecting to {WiFiCreds.SSID}", WifiCreds.SSID);
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
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception : {ex}\nMessage:{ex.Message}", ex, ex.Message);
                throw;
            }

            return Page();
        }
        public async Task<IActionResult> OnPostUserManagementSetRootPSWDAsync()
        {
            if (!ModelState.IsValid) { return Page(); }
            try
            {
                if (UserCreds.Password != null && UserCreds.Password.Length > 0)
                {
                    string output = "";
                    int status = await Utils.RunShellAsync("/opt", "changepasswd.sh", "root " + UserCreds.Password, _logger, ref output);
                    if (status == 0)
                    {
                        _logger.LogTrace("set new root password");
                        Message = "new root password is set";
                    }
                    else
                    {
                        Message = "Error while setting new root password";
                        _logger.LogError("{}", Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception : {ex}\nMessage:{ex.Message}", ex, ex.Message);
                throw;
            }
            return Page();
        }
        public async Task<IActionResult> OnPostUserManagementCreateUserAsync()
        {
            if (!ModelState.IsValid) { return Page(); }
            try
            {
                if (UserCreds.User != null && UserCreds.User.Length > 0 && UserCreds.Password != null && UserCreds.Password.Length > 0)
                {
                    string output = "";
                    int status = await Utils.RunShellAsync("/opt", "addsudouser.sh", UserCreds.User + " " + UserCreds.Password, _logger, ref output);
                    if (status == 0)
                    {
                        _logger.LogTrace("added new user {UserCreds.User}", UserCreds.User);
                        Message = "added new user";
                    }
                    else
                    {
                        Message = "Error while adding new user";
                        _logger.LogError("{}", Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception : {ex}\nMessage:{ex.Message}", ex, ex.Message);
                throw;
            }
            return Page();
        }
    }
}
