
using System.CommandLine;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace DDNS_API_Server
{
    class Program
    {
        private static string? LogFilePath = null;

        static int Main(string[] args)
        {
            // Create a root command with some options
            Option<int> portOption = new(
                new string[] { "-p", "--port" },
                description: "The port which the server will be listening on")
            {
                IsRequired = true
            };
            Option<FileInfo> logFileOption = new(
                    new string[] { "-l", "--log-file" },
                    "The path to the log file")
            {
                IsRequired = true
            };
            RootCommand rootCommand = new()
            {
                portOption,
                logFileOption,
            };

            rootCommand.Description = "DDNS API Server";

            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.SetHandler(RunServer, portOption, logFileOption);

            // Parse the incoming args and invoke the handler
            return rootCommand.Invoke(args);
        }

        static void RunServer(int port, FileInfo logFile)
        {
            Console.WriteLine($"Port:\t{port}");
            Console.WriteLine($" Log:\t{logFile.FullName}");
            Console.WriteLine();

            if(!logFile.Directory!.Exists)
            {
                Directory.CreateDirectory(logFile.Directory.FullName);
            }

            LogFilePath = logFile.FullName;

            HttpListener httpListener = new(new System.Net.IPAddress(0), port);
            httpListener.Request += HttpListener_Request;
            httpListener.Start();
            Console.WriteLine($"Start listening on port {port}");

            ManualResetEvent manualResetEvent = new(false);
            manualResetEvent.WaitOne();
        }

        private static void AppendLog(string text)
        {

            string str = $"[{DateTime.Now}] [Info] {text}";
            Console.WriteLine(str);
            Log(str);
        }

        private static void AppendError(string text)
        {
            string str = $"[{DateTime.Now}] [Error] {text}";
            Console.Error.WriteLine(str);
            Log(str);
        }

        private static readonly object logObj = new();
        private static void Log(string text)
        {
            if (LogFilePath != null)
            {
                lock (logObj)
                {
                    using StreamWriter streamWriter = File.AppendText(LogFilePath);
                    streamWriter.WriteLine(text);
                }
            }
        }

        public class DDNSRequest
        {
            public string Host { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public int TTL { get; set; }
            public string Data { get; set; } = string.Empty;
        }

        private static void HttpListener_Request(object? sender, HttpListenerRequestEventArgs context)
        {
            try
            {
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                try
                {
                    string[] segments = request.RequestUri.Segments;

                    string xForwardFor = string.Empty;
                    if (request.Headers.ContainsKey("X-Forwarded-For"))
                    {
                        xForwardFor = request.Headers["X-Forwarded-For"];
                        xForwardFor += ", ";
                    }

                    AppendLog($"[Request] {xForwardFor}{context.Request.RemoteEndpoint} ({context.Request.Method}) {context.Request.RequestUri}");

                    StreamReader streamReader = new StreamReader(request.InputStream, Encoding.UTF8);
                    JsonSerializerOptions options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    DDNSRequest ddnsRequest = JsonSerializer.Deserialize<DDNSRequest>(streamReader.ReadToEnd(), options)!;

                    StringBuilder reportBuilder = new StringBuilder();
                    int error = 0;
                    try
                    {
                        using (Process nsupdateProcess = new Process())
                        {
                            nsupdateProcess.StartInfo.FileName = "nsupdate";
                            nsupdateProcess.StartInfo.ArgumentList.Add("-l");
                            nsupdateProcess.StartInfo.CreateNoWindow = true;
                            nsupdateProcess.StartInfo.UseShellExecute = false;
                            nsupdateProcess.StartInfo.RedirectStandardOutput = true;
                            nsupdateProcess.StartInfo.RedirectStandardError = true;
                            nsupdateProcess.StartInfo.RedirectStandardInput = true;
                            nsupdateProcess.Start();

                            void NsupdateProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
                            {
                                AppendLog($"[Client] {e.Data}");
                                reportBuilder.AppendLine($"[Info] [Client] {e.Data}");
                            }

                            void NsupdateProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
                            {
                                AppendError($"[Client] {e.Data}");
                                reportBuilder.AppendLine($"[Error] [Client] {e.Data}");
                                error++;
                            }

                            nsupdateProcess.OutputDataReceived += NsupdateProcess_OutputDataReceived;
                            nsupdateProcess.ErrorDataReceived += NsupdateProcess_ErrorDataReceived;

                            void StdWriteLine(string line)
                            {
                                AppendLog($"[Action] {line}");
                                reportBuilder.AppendLine($"[Info] [Action] {line}");
                                nsupdateProcess.StandardInput.WriteLine(line);
                            }

                            StdWriteLine($"update delete {ddnsRequest.Host} {ddnsRequest.Type}");
                            StdWriteLine($"update add {ddnsRequest.Host} {ddnsRequest.TTL} {ddnsRequest.Type} {ddnsRequest.Data}");
                            StdWriteLine($"send");
                            StdWriteLine($"quit");

                            nsupdateProcess.OutputDataReceived -= NsupdateProcess_OutputDataReceived;
                            nsupdateProcess.ErrorDataReceived -= NsupdateProcess_ErrorDataReceived;
                        }
                            
                    }
                    catch (Exception ex)
                    {
                        AppendError($"[Process] {ex}");
                        reportBuilder.AppendLine($"[Error] [Process] {ex}");
                        error++;
                    }

                    // response
                    response.WriteContent(reportBuilder.ToString());

                    if(error > 0)
                    {
                        response.InternalServerError();
                    }
                }
                catch (Exception ex)
                {
                    AppendError($"[Request] {ex}");
                    response.WriteContent($"[Error] [Request] {ex}");
                    response.InternalServerError();
                }
                finally
                {
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                AppendError(ex.ToString());
            }
        }
       
    }
}
