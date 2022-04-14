﻿using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
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
using FileSystem = Microsoft.VisualBasic.FileIO.FileSystem;

namespace Rippr
{
    public class RippingService
    {
        private RipprOptions ripprOptions;

        public RippingService()
        {
            this.ripprOptions = RipprOptions.getDefault();
        }

        public RippingService(RipprOptions ripprOptions)
        {
            this.ripprOptions = ripprOptions;
        }

        [DllImport("winmm.dll")]
        static extern Int32 mciSendString(string command, string buffer, int bufferSize, IntPtr hwndCallback);

        public async Task<List<DiscInfo>> GetDiscInfoList()
        {
            ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM Win32_CDROMDrive");

            var discInfoList = new List<DiscInfo>();

            foreach (ManagementObject mo in mos.Get())
            {
                var discInfo = new DiscInfo();
                discInfo.DriveLetter = mo.GetPropertyValue("Drive").ToString();
                long result;
                var didParseSize = Int64.TryParse(mo.GetPropertyValue("Size").ToString(), out result);
                if (didParseSize && result != 0)
                {
                    var typeOfDisc = GetDiscTypeFromSize(result);
                    discInfo.DiscType = typeOfDisc;
                    String queryString = mo.GetPropertyValue("VolumeName").ToString();
                    discInfo.MediaInfo = await GetMediaInfoFromAPI(queryString, typeOfDisc, discInfo.DriveLetter);
                }
                else
                {
                    var driveInfo = new DriveInfo(discInfo.DriveLetter);
                    if (driveInfo.DriveFormat == "CDFS" || driveInfo.VolumeLabel.Contains("Audio"))
                    {
                        discInfo.DiscType = "CD";
                        var mediaInfo = new MediaInformation();
                        mediaInfo.Type = "music";
                        discInfo.MediaInfo = mediaInfo;
                    }
                    else
                    {
                        throw new Exception("Failed to parse disc size!");
                    }
                }

                discInfoList.Add(discInfo);
            }

            return discInfoList;
        }

        public void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            FileSystem.CopyDirectory(sourcePath, targetPath, UIOption.AllDialogs);
        }

        public void MoveToOutputDir(List<DiscInfo> discInfo)
        {
            foreach (var disc in discInfo)
            {
                var pathInfo = getPathInfo(disc);
                var sourcePath = pathInfo.InputPath;
                var outputPath = getOutputPath(disc);
                var localPath = String.Format(@"{0}\{1}", sourcePath,
                    String.Format("{0} ({1})", disc.MediaInfo.Title, disc.MediaInfo.Year));

                if (Directory.Exists(localPath))
                {
                    if (Directory.Exists(outputPath))
                    {
                        if (ripprOptions.IsDebugMode)
                        {
                            Console.WriteLine(String.Format("Would copy {0} to {1}, recursively", sourcePath,
                                outputPath));
                        }
                        else
                        {
                            CopyFilesRecursively(localPath, outputPath);
                            Directory.Delete(localPath, true);
                        }
                    }
                }
            }
        }

        public void RipEachDisc(List<DiscInfo> discInfo)
        {
            // Command : "C:\Program Files (x86)\MakeMKV\makemkvcon64.exe" --minlength=130 -r --decrypt --directio=true mkv disc:0 all "C:\ProgramData\Rips\Jack Reacher (2012)"
            foreach (var (disc, index) in discInfo.Select((value, index) => (value, index)))
            {
                String command;
                var pathInfo = getPathInfo(disc);
                if (disc.DiscType != "CD")
                {
                    var tempDirForRip =
                        CreateTempDirForRip(String.Format(@"{0} ({1})", disc.MediaInfo.Title, disc.MediaInfo.Year),
                            disc);
                    var runTimeInSeconds = GetRuntimeInSeconds(disc.MediaInfo.Runtime);
                    command = String.Format(pathInfo.RipperExeOpts, pathInfo.RipperExePath, runTimeInSeconds, index,
                        tempDirForRip);
                }
                else
                {
                    command = String.Format(pathInfo.RipperExeOpts, pathInfo.RipperExePath, disc.DriveLetter,
                        ripprOptions.OutputOpts.MusicOutputPath);
                }

                if (ripprOptions.IsDebugMode)
                {
                    Console.WriteLine($"Would run this command: {command}");
                }
                else
                {
                    StartRip(command);
                    EjectDisc(discInfo, index);
                }
            }
        }

        public String getOutputPath(DiscInfo disc)
        {
            var mediaType = disc.MediaInfo.Type;
            var discType = disc.DiscType;
            return mediaType == "movie" ? discType == "Blu-Ray" ? ripprOptions.OutputOpts.HDMovieOutputPath :
                ripprOptions.OutputOpts.SDMovieOutputPath :
                mediaType == "tv" ? discType == "Blu-Ray" ? ripprOptions.OutputOpts.HDTVOutputPath :
                ripprOptions.OutputOpts.SDTVOutputPath :
                mediaType == "music" ? ripprOptions.OutputOpts.MusicOutputPath : ripprOptions.OutputOpts.ISOOutputPath;
        }

        public RipprInputOpts getPathInfo(DiscInfo disc)
        {
            return disc.DiscType == "CD" ? ripprOptions.CdInputOpts :
                disc.DiscType == "DVD" ? ripprOptions.DvdInputOpts : ripprOptions.BluRayInputOpts;
        }

        public void EjectDisc(List<DiscInfo> discInfo, int index)
        {
            // TODO: Figure out how to target disc for open
            string rt = "";
            mciSendString("set CDAudio door closed", rt, 127, IntPtr.Zero);
        }

        public void StartRip(String command)
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

        public string CreateTempDirForRip(String dirName, DiscInfo disc)
        {
            var baseTempDir = getPathInfo(disc).InputPath;
            var dirToCreate = $@"{baseTempDir}\{dirName}";
            if (!Directory.Exists(dirToCreate))
            {
                if (ripprOptions.IsDebugMode)
                {
                    Console.WriteLine($@"Would create {dirToCreate}");
                }
                else
                {
                    Directory.CreateDirectory(dirToCreate);
                }
            }

            return dirToCreate;
        }

        public string GetRuntimeInSeconds(string runtime)
        {
            var runtimeInMinutesStr = Regex.Replace(runtime, "[a-zA-Z]", "").Trim();
            int runtimeInMinutes = int.Parse(runtimeInMinutesStr);
            return (runtimeInMinutes * 60).ToString();
        }


        public async Task<MediaInformation> GetMediaInfoFromAPI(String queryString, string typeOfDisc,
            string driveLetter)
        {
            var mediaInfo = new MediaInformation();

            if (typeOfDisc != "CD")
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        string requestUri = String.Format("https://www.omdbapi.com/?apikey={0}&t={1}",
                            ripprOptions.OmdbApiKey, queryString);
                        using (HttpResponseMessage response = await client.GetAsync(requestUri))
                        {
                            if (response.StatusCode >= System.Net.HttpStatusCode.OK &&
                                response.StatusCode < System.Net.HttpStatusCode.BadRequest)
                            {
                                using (HttpContent content = response.Content)
                                {
                                    string contentString = await content.ReadAsStringAsync();
                                    dynamic responseObj = JsonConvert.DeserializeObject(contentString);
                                    mediaInfo.Title = responseObj.Title;
                                    mediaInfo.Year = responseObj.Year;
                                    mediaInfo.Type = responseObj.Type;
                                    mediaInfo.Runtime = responseObj.Runtime;
                                    return mediaInfo;
                                }
                            }
                            else
                            {
                                // TODO: drop to search here
                                return null;
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
            else
            {
                Console.WriteLine(
                    "It's a CD, skipping because ripper for CDs should handle identification from FreeDB");
                mediaInfo.Type = "music";
                return mediaInfo;
            }
        }

        // https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp


        public String GetDiscTypeFromSize(long size)
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