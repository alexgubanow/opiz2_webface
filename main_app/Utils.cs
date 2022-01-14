using main_app.Pages;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace main_app
{
    public class Utils
    {
        public static Task<int> RunProcessAsync(string fileName, string args, ILogger<IndexModel> logger, ref string output)
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
                logger.LogError("Started task [{fileName} {args}] rised error\n{output}", fileName, args, error);
            }
            logger.LogTrace("Started task [{fileName} {args}]\nOutput [{output}]", fileName, args, output);
            tcs.SetResult(process.ExitCode);
            process.Dispose();
            return tcs.Task;
        }
    }
}
