using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace spam
{
    public delegate void ReceivedEventHandler(object sender, string e);

    class Program
    {
        private static readonly object gate = new object();

        static void Main(string[] args)
        {
            int numberOfProcesses = 5;
            string spamSleepInSeconds = "0.1";
            int numberOfSleepTests = 3;
            int sleepTestDelay = 500;
            int sleepTestTolerance = 100;
            bool redirect = true;

            Console.WriteLine($@"Start sleep test. {{
                numberOfProcesses: {numberOfProcesses},
                spamSleepInSeconds: {spamSleepInSeconds},
                numberOfSleepTests: {numberOfSleepTests},
                sleepTestDelay: {sleepTestDelay},
                sleepTestTolerance: {sleepTestTolerance},
                redirect: {redirect}
            }}");
            Console.WriteLine("");

            (new Program()).Run(
                numberOfProcesses,
                spamSleepInSeconds,
                numberOfSleepTests,
                sleepTestDelay,
                sleepTestTolerance,
                redirect
            ).GetAwaiter().GetResult();
        }

        public async Task Run(
            int numberOfProcess,
            string spamSleepInSeconds,
            int numberOfSleepTests,
            int sleepTestDelay,
            int sleepTestTolerance,
            bool redirect
        )
        {
            var processes = new List<Process>();
            for (var i = 0; i < numberOfProcess; i++)
            {
                var p = this.RunProcess(spamSleepInSeconds, redirect);
                processes.Add(p);
            }

            var tasks = new List<Task>();
            for (var i = 0; i < numberOfSleepTests; i++)
            {
                tasks.Add(SleepTest(sleepTestDelay, sleepTestTolerance));
            }

            while(true)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10));
            }
        }

        public async Task BeginRead(Process process, StreamReader streamReader, ReceivedEventHandler handler)
        {
            while(true)
            {
                string data = null;
                try
                {
                    data = await streamReader.ReadLineAsync().ConfigureAwait(false);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    throw e;
                }
                catch (ObjectDisposedException)
                {
                    break; // Treat as EOF
                }
                catch (InvalidOperationException)
                {
                    // Try again next time
                }

                if (data != null)
                {
                    handler(process, data);
                }
                await Task.Delay(1); // very important otherwise the error still occurs
            }
        }

        private async Task SleepTest(int sleepTestDelay, int sleepTestTolerance)
        {
            while (true)
            {
                var now = DateTime.UtcNow;
                await Task.Delay(sleepTestDelay).ConfigureAwait(false);
                Console.WriteLine("Test");
                var s = DateTime.UtcNow - now;
                if (s > TimeSpan.FromMilliseconds(sleepTestDelay) + TimeSpan.FromMilliseconds(sleepTestTolerance))
                {
                    Console.WriteLine("------- SLEPT TOO LONG: " + s.TotalMilliseconds);
                }
            }
        }

        private Process RunProcess(string time, bool redirect)
        {
            var process = new Process();
            process.StartInfo.FileName = Path.Join(Directory.GetCurrentDirectory(), "spam.sh");
            process.StartInfo.Arguments = time;
            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            if (redirect)
            {
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.EnableRaisingEvents = true;
                //process.OutputDataReceived += HandleOutputData;
                //process.ErrorDataReceived += HandleErrorData;
                process.Exited += HandleProcessExited;
            }

            process.Start();

            if (redirect)
            {
                //process.BeginOutputReadLine();
                //process.BeginErrorReadLine();
                BeginRead(process, process.StandardOutput, this.HandleOutputData);
                BeginRead(process, process.StandardError, this.HandleErrorData);
            }

            return process;
        }

        private void HandleProcessExited(object sender, EventArgs e)
        {
        }

        private void HandleOutputData(Object obj, string data)
        {
            if (data != null) {
                //Console.WriteLine($"out: {data}");
            }
        }

        private void HandleErrorData(Object obj, string data)
        {
            if (data != null) {
                //Console.WriteLine($"err: {data}");
            }
        }
    }
}
