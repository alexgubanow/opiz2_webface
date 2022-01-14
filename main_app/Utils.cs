using main_app.Pages;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace main_app
{
    public class Utils
    {
        public static Task<int> RunProcessAsync(string dir, string fileName, string args, ILogger<IndexModel> logger, ref string output)
        {
            output = "";
            var tcs = new TaskCompletionSource<int>();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = fileName,
                    Arguments = args,
                    WorkingDirectory = dir,
                    UseShellExecute = dir != "",
                    RedirectStandardOutput = dir == "",
                    RedirectStandardError = dir == ""
                },
                EnableRaisingEvents = true
            };
            process.Start();
            if (process.StartInfo.RedirectStandardError)
            {
                string error = process.StandardError.ReadToEnd();
                if (error != null && error.Length > 0)
                {
                    logger.LogError("Started task [{fileName} {args}] rised error\n{output}", fileName, args, error);
                }
            }
            var tmpString = new StringBuilder();
            while (!process.HasExited)
            {
                if (process.StartInfo.RedirectStandardOutput)
                {
                    tmpString.Append(process.StandardOutput.ReadToEnd());
                    output = tmpString.ToString();
                }
            }
            logger.LogTrace("Started task [{fileName} {args}]\nOutput [{output}]", fileName, args, output);
            tcs.SetResult(process.ExitCode);
            process.Dispose();
            return tcs.Task;
        }
        public static Task<int> RunShellAsync(string dir, string fileName, string args, ILogger<IndexModel> logger, ref string output)
        {
            return RunProcessAsync(dir, fileName, args, logger, ref output);
        }
        public static Task<int> RunProcessAsync(string fileName, string args, ILogger<IndexModel> logger, ref string output)
        {
            return RunProcessAsync("", fileName, args,logger, ref  output);
        }
    }
}
