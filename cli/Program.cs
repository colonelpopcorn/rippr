using Newtonsoft.Json;
using OpticalDiscAutomator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace cli
{
    class Program
    {
        [DllImport("winmm.dll")]
        static extern Int32 mciSendString(string command, string buffer, int bufferSize, IntPtr hwndCallback);

        static void Main(string[] args)
        {
                        //DoRip(args).Wait();
        }

        static async Task DoRip(string[] args)
        {
            Console.WriteLine("Identifying disc(s)...");

            var discInfo = await GetDiscInfoList();

            Console.WriteLine("Identification done, starting rip...");

            RipEachDisc(discInfo);

            Console.WriteLine("Ripping is done, starting move...");

            //MoveToOutputDir(discInfo);

        }

        private static void MoveToOutputDir(List<DiscInfo> discInfo)
        {
            foreach (var disc in discInfo)
            {
                var localPath = String.Format(@"C:\ProgramData\Rips\{0}", String.Format("{0} ({1})", disc.MediaInfo.Title, disc.MediaInfo.Year));
                var remotePath = @"Z:\Docker\HandBrake\watch";

                if (Directory.Exists(localPath))
                {
                    if (Directory.Exists(remotePath))
                    {
                        CopyFilesRecursively(localPath, remotePath);
                        //Directory.Delete(localPath, true);
                    }
                }
            }
            
        }

        private static void RipEachDisc(List<DiscInfo> discInfo)
        {
            // Command : "C:\Program Files (x86)\MakeMKV\makemkvcon64.exe" --minlength=130 -r --decrypt --directio=true mkv disc:0 all "C:\ProgramData\Rips\Jack Reacher (2012)"
            var makeMKVPath = @"C:\Program Files (x86)\MakeMKV\makemkvcon64.exe";
            foreach (var (disc, index) in discInfo.Select((value, index) => (value, index)))
            {
                var tempDirForRip = CreateTempDirForRip(String.Format(@"{0} ({1})", disc.MediaInfo.Title, disc.MediaInfo.Year));
                var runTimeInSeconds = GetRuntimeInSeconds(disc.MediaInfo.Runtime);
                var command = String.Format(@"""{0}"" --minlength={1} -r --decrypt --directio=true mkv disc:{2} all ""{3}""", makeMKVPath, runTimeInSeconds, index, tempDirForRip);
                //StartRip(command);
                EjectDisc(discInfo, index);
            }
        }

        private static void EjectDisc(List<DiscInfo> discInfo, int index)
        {
            // TODO: Figure out how to target disc for open
            string rt = "";
            mciSendString("set CDAudio door closed", rt, 127, IntPtr.Zero);
        }

        private static void StartRip(String command)
        {

            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            //* Set your output and error (asynchronous) handlers
            process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
            process.Start();
            process.StandardInput.WriteLine(command);
            process.StandardInput.Flush();
            process.StandardInput.Close();

            //* Start process and handlers
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }

        static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            //* Do your stuff with the output (write to console/log/StringBuilder)
            Console.WriteLine(outLine.Data);
        }

        private static string CreateTempDirForRip(String dirName)
        {
            var baseTempDir = @"C:\ProgramData\Rips";
            var dirToCreate = String.Format(@"{0}\{1}", baseTempDir, dirName);
            if (!Directory.Exists(dirToCreate))
            {
                Directory.CreateDirectory(dirToCreate);
            }
            return dirToCreate;
        }

        private static string GetRuntimeInSeconds(string runtime)
        {
            var runtimeInMinutesStr = Regex.Replace(runtime, "[a-zA-Z]", "").Trim();
            int runtimeInMinutes = int.Parse(runtimeInMinutesStr);
            return (runtimeInMinutes * 60).ToString();
        }

        private static async Task<List<DiscInfo>> GetDiscInfoList()
        {
            ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM Win32_CDROMDrive");

            var discInfoList = new List<DiscInfo>();

            foreach (ManagementObject mo in mos.Get())
            {
                var discInfo = new DiscInfo();
                long result;
                var didParseSize = Int64.TryParse(mo.GetPropertyValue("Size").ToString(), out result);
                if (didParseSize)
                {
                    var typeOfDisc = GetDiscTypeFromSize(result);
                    discInfo.DiscType = typeOfDisc;
                    String queryString = mo.GetPropertyValue("VolumeName").ToString();
                    discInfo.MediaInfo = await GetMediaInfoFromAPI(queryString);
                }
                else
                {
                    throw new Exception("Failed to parse disc size!");
                }
                discInfoList.Add(discInfo);
            }
            return discInfoList;
        }

        private static async Task<MediaInformation> GetMediaInfoFromAPI(String queryString)
        {
            var mediaInfo = new MediaInformation();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var apiKey = "4ec7e2a&t";
                    string requestUri = String.Format("https://www.omdbapi.com/?apikey={0}={1}", apiKey, queryString);
                    using (HttpResponseMessage response = await client.GetAsync(requestUri))
                    {
                        Console.WriteLine("Response status code: " + response.StatusCode);
                        Console.WriteLine("Response: " + response.Content);
                        using (HttpContent content = response.Content)
                        {
                            string contentString = await content.ReadAsStringAsync();
                            dynamic responseObj = JsonConvert.DeserializeObject(contentString);
                            Console.WriteLine(contentString);
                            mediaInfo.Title = responseObj.Title;
                            mediaInfo.Year = responseObj.Year;
                            mediaInfo.Type = responseObj.Type;
                            mediaInfo.Runtime = responseObj.Runtime;
                            return mediaInfo;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                return null;
            }
        }

        // https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            var lastNode = sourcePath.Split('\\').Last();
            var newTargetPath = (targetPath + "\\" + lastNode);
            if (!Directory.Exists(newTargetPath))
            {
                Directory.CreateDirectory(newTargetPath);
            }
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                var newDirPath = dirPath.Replace(sourcePath, newTargetPath);
                if (!Directory.Exists(newDirPath))
                {
                    Console.WriteLine("Creating " + newDirPath);
                    Directory.CreateDirectory(newDirPath);
                }
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                var newFilePath = newPath.Replace(sourcePath, newTargetPath);
                if (!File.Exists(newFilePath))
                {
                    Console.WriteLine("Moving " + newPath + " to " + newFilePath);
                    File.Copy(newPath, newFilePath, true);
                }
            }
        }

        private static String GetDiscTypeFromSize(long size)
        {
            if (size > 650000000 && size < 4700000000)
            {
                return "CD";
            }

            if (size > 4700000000 && size < 1.708e+10)
            {
                return "DVD";
            }

            if (size > 1.708e+10 && size < 128000000000)
            {
                return "Blu-Ray";
            }

            else
            {
                return "Unknown";
            }
        }
    }
}