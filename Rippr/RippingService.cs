using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Rippr;

public class RippingService
{
    private readonly int MIN_MINUTE_REMOVAL_MOVIE = 5 * 60;
    private readonly int MIN_MINUTE_REMOVAL_TV = 2 * 60;
    private readonly string MOVIE_TYPE = "movie";
    private readonly string TELEVISION_TYPE = "series";
    private readonly string MUSIC_TYPE = "music";
    private readonly RipprOptions ripprOptions;

    public RippingService()
    {
        ripprOptions = RipprOptions.getDefault();
    }

    public RippingService(RipprOptions ripprOptions)
    {
        this.ripprOptions = ripprOptions;
    }

    [DllImport("winmm.dll")]
    private static extern int mciSendString(string command, string buffer, int bufferSize, IntPtr hwndCallback);

    public async Task<List<DiscInfo>> GetDiscInfoList(string driveLetter)
    {
        var mos = new ManagementObjectSearcher($"SELECT * FROM Win32_CDROMDrive where Drive = '{driveLetter}'");

        var discInfoList = new List<DiscInfo>();

        foreach (ManagementObject mo in mos.Get())
        {
            var discInfo = new DiscInfo();
            discInfo.DriveLetter = mo.GetPropertyValue("Drive").ToString();
            long result;
            var didParseSize = long.TryParse(mo.GetPropertyValue("Size").ToString(), out result);
            if (didParseSize && result != 0)
            {
                var typeOfDisc = GetDiscTypeFromSize(result);
                discInfo.DiscType = typeOfDisc;
                var queryString = mo.GetPropertyValue("VolumeName").ToString().ToLower().Replace("_", " ");
                discInfo.MediaInfo = await GetMediaInfoFromAPI(queryString, typeOfDisc);
            }
            else
            {
                var driveInfo = new DriveInfo(discInfo.DriveLetter);
                if (driveInfo.DriveFormat == "CDFS" || driveInfo.VolumeLabel.Contains("Audio"))
                {
                    discInfo.DiscType = "CD";
                    var mediaInfo = new MediaInformation();
                    mediaInfo.Type = MUSIC_TYPE;
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
        Directory.CreateDirectory(targetPath);
        FileSystem.CopyDirectory(sourcePath, targetPath, UIOption.AllDialogs);
    }

    public void MoveToOutputDir(List<DiscInfo> discInfo)
    {
        foreach (var disc in discInfo)
        {
            var pathInfo = getPathInfo(disc);
            var sourcePath = pathInfo.InputPath;
            var mediaFolderName = String.Join(@"\", GetPathSchemeByType(disc));
            var outputPath = getOutputPath(disc, mediaFolderName);
            var localPath = $@"{sourcePath}\{mediaFolderName}";

            if (Directory.Exists(localPath))
            {
                if (ripprOptions.IsDebugMode)
                {
                    Console.WriteLine("Would copy {0} to {1}, recursively", sourcePath, outputPath);
                    RenameFilesInDirectory(localPath, disc);
                }
                else
                {
                    if (disc.MediaInfo.Type == TELEVISION_TYPE)
                    {
                        RenameFilesInDirectory(localPath, disc);
                    }
                    CopyFilesRecursively(localPath, outputPath);
                    Directory.Delete(localPath, true);
                }
            }
            else
            {
                Console.WriteLine("Error: Could not find output of ripping process. Something must have gone wrong!");
            }
        }
    }

    private void RenameFilesInDirectory(string localPath, DiscInfo disc)
    {
        DirectoryInfo d = new DirectoryInfo(localPath);
        FileInfo[] fileInformationArray = d.GetFiles("*");
        var index = 0;
        var seasonNumberStr = disc.MediaInfo.SeasonNumber.ToString().Length == 1 ? $"0{disc.MediaInfo.SeasonNumber}" : disc.MediaInfo.SeasonNumber.ToString();
        foreach(FileInfo fileInfo in fileInformationArray)
        {
            var episodeNumber = disc.MediaInfo.EpisodeStart + index;
            var episodeNumberStr = episodeNumber.ToString().Length == 1 ? $"0{episodeNumber}" : episodeNumber.ToString();
            var newEpisodeName =
                $"{disc.MediaInfo.Title} - S{seasonNumberStr}E{episodeNumberStr}{fileInfo.Extension}";
            if (ripprOptions.IsDebugMode)
            {
                Console.WriteLine($"Would rename file from {fileInfo.Name} to -> {newEpisodeName}");
                
            }
            else
            {
                // Do the renaming here
                File.Move(fileInfo.FullName, Path.Combine(fileInfo.DirectoryName, newEpisodeName));
            }
            index++;
        }
    }

    public void RipEachDisc(List<DiscInfo> discInfo)
    {
        // Command : "C:\Program Files (x86)\MakeMKV\makemkvcon64.exe" --minlength=130 -r --decrypt --directio=true mkv disc:0 all "C:\ProgramData\Rips\Jack Reacher (2012)"
        foreach (var (disc, index) in discInfo.Select((value, index) => (value, index)))
        {
            string command;
            var pathInfo = getPathInfo(disc);
            if (disc.DiscType != "CD")
            {
                var tempDirByType = String.Join(@"\", GetPathSchemeByType(disc));
                var tempDirForRip = CreateTempDirForRip(tempDirByType, disc);
                var runTimeInSeconds = GetRuntimeInSeconds(disc.MediaInfo.Runtime, disc.MediaInfo.Type);
                command = string.Format(pathInfo.RipperExeOpts, pathInfo.RipperExePath, runTimeInSeconds, index,
                    tempDirForRip);
            }
            else
            {
                command = string.Format(pathInfo.RipperExeOpts, pathInfo.RipperExePath, disc.DriveLetter,
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

    public string[] GetPathSchemeByType(DiscInfo disc)
    {
        return disc.MediaInfo.Type == MOVIE_TYPE ? new[] {RemoveInvalidFilePathCharacters($"{disc.MediaInfo.Title} ({disc.MediaInfo.Year})", "-")} : disc.MediaInfo.Type == TELEVISION_TYPE ? getTelevisionPathFromUser(disc) : new[] {RemoveInvalidFilePathCharacters($"{disc.MediaInfo.Title}", "-")}; }

    private string[] getTelevisionPathFromUser(DiscInfo disc)
    {
        if (disc.MediaInfo.SeasonNumber == 0)
        {
            Console.WriteLine("TV series disc detected, enter season #:");
            var seasonNumberStr = Console.ReadLine();
            int seasonNumber;
            if (!Int32.TryParse(seasonNumberStr, out seasonNumber))
            {
                throw new Exception("Failed to get season number!");
            }
            Console.WriteLine("Enter the first episode number on the disc:");
            var startEpisodeStr = Console.ReadLine();
            int startEpisode;
            if (!Int32.TryParse(startEpisodeStr, out startEpisode))
            {
                throw new Exception("Failed to get episode start number!");
            }
            Console.WriteLine("Enter the last episode number on the disc:");
            var lastEpisodeStr = Console.ReadLine();
            int lastEpisode;
            if (!Int32.TryParse(lastEpisodeStr, out lastEpisode))
            {
                throw new Exception("Failed to get episode end number!");
            }

            disc.MediaInfo.SeasonNumber = seasonNumber;
            disc.MediaInfo.EpisodeStart = startEpisode;
            disc.MediaInfo.EpisodeEnd = lastEpisode;
        }

        var finalSeasonNumberStr = disc.MediaInfo.SeasonNumber.ToString(); 
        var finalSeasonNumber = finalSeasonNumberStr.Length > 1 ? finalSeasonNumberStr : $"0{finalSeasonNumberStr}";
        return new[] {disc.MediaInfo.Title, $"Season {finalSeasonNumber}" };
    }

    // https://stackoverflow.com/questions/3825433/c-sharp-remove-invalid-characters-from-filename
    public static string RemoveInvalidFilePathCharacters(string filename, string replaceChar)
    {
        string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
        return r.Replace(filename, replaceChar);
    }

    public string getOutputPath(DiscInfo disc)
    {
        var mediaType = disc.MediaInfo.Type;
        var discType = disc.DiscType;
        return mediaType == MOVIE_TYPE ? discType == "Blu-Ray" ? ripprOptions.OutputOpts.HDMovieOutputPath :
            ripprOptions.OutputOpts.SDMovieOutputPath :
            mediaType == TELEVISION_TYPE ? discType == "Blu-Ray" ? ripprOptions.OutputOpts.HDTVOutputPath :
            ripprOptions.OutputOpts.SDTVOutputPath :
            mediaType == MUSIC_TYPE ? ripprOptions.OutputOpts.MusicOutputPath : ripprOptions.OutputOpts.ISOOutputPath;
    }

    public string getOutputPath(DiscInfo disc, string mediaFolderName)
    {
        var mediaType = disc.MediaInfo.Type;
        var discType = disc.DiscType;
        return mediaType == MOVIE_TYPE ? discType == "Blu-Ray" ? $@"{ripprOptions.OutputOpts.HDMovieOutputPath}\{mediaFolderName}":
            $@"{ripprOptions.OutputOpts.SDMovieOutputPath}\{mediaFolderName}" :
            mediaType == TELEVISION_TYPE ? discType == "Blu-Ray" ? $@"{ripprOptions.OutputOpts.HDTVOutputPath}\{mediaFolderName}" :
            $@"{ripprOptions.OutputOpts.SDTVOutputPath}\{mediaFolderName}" :
            mediaType == MUSIC_TYPE ? $@"{ripprOptions.OutputOpts.MusicOutputPath}\{mediaFolderName}" : $@"{ripprOptions.OutputOpts.ISOOutputPath}\{mediaFolderName}";
    }

    public RipprInputOpts getPathInfo(DiscInfo disc)
    {
        return disc.DiscType == "CD" ? ripprOptions.CdInputOpts :
            disc.DiscType == "DVD" ? ripprOptions.DvdInputOpts : ripprOptions.BluRayInputOpts;
    }

    public void EjectDisc(List<DiscInfo> discInfo, int index)
    {
        // TODO: Figure out how to target disc for open
        var rt = "";
        mciSendString("set CDAudio door open", rt, 127, IntPtr.Zero);
    }

    public void StartRip(string command)
    {
        var process = new Process();
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        //* Set your output and error (asynchronous) handlers
        process.OutputDataReceived += OutputHandler;
        process.ErrorDataReceived += OutputHandler;
        process.Start();
        process.StandardInput.WriteLine(command);
        process.StandardInput.Flush();
        process.StandardInput.Close();

        //* Start process and handlers
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
    }

    private static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
    {
        //* Do your stuff with the output (write to console/log/StringBuilder)
        Console.WriteLine(outLine.Data);
    }

    public string CreateTempDirForRip(string dirName, DiscInfo disc)
    {
        var baseTempDir = getPathInfo(disc).InputPath;
        var dirToCreate = $@"{baseTempDir}\{dirName}";
        if (!Directory.Exists(dirToCreate))
        {
            if (ripprOptions.IsDebugMode)
                Console.WriteLine($@"Would create {dirToCreate}");
            else
                Directory.CreateDirectory(dirToCreate);
        }

        return dirToCreate;
    }

    public string GetRuntimeInSeconds(string runtime, string type)
    {
        try
        {
            var runtimeInMinutesStr = Regex.Replace(runtime, "[a-zA-Z]", "").Trim();
            var runtimeInMinutes = int.Parse(runtimeInMinutesStr);
            return (runtimeInMinutes * 60 - (type == MOVIE_TYPE ? MIN_MINUTE_REMOVAL_MOVIE : MIN_MINUTE_REMOVAL_TV))
                .ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return "0";
        }
    }


    public async Task<MediaInformation> GetMediaInfoFromAPI(string queryString, string typeOfDisc)
    {
        var mediaInfo = new MediaInformation();

        if (typeOfDisc != "CD")
            try
            {
                if (!ripprOptions.IsSearchMode)
                {
                    var requestUri = string.Format("https://www.omdbapi.com/?apikey={0}&t={1}",
                        ripprOptions.OmdbApiKey, queryString);
                    dynamic response = await getDiscDetailsByName(requestUri);
                    mediaInfo.Title = response.Title;
                    mediaInfo.Year = response.Year;
                    mediaInfo.Type = response.Type;
                    mediaInfo.Runtime = response.Runtime;
                }

                if (mediaInfo.IsEmpty())
                {
                    if (!ripprOptions.IsSearchMode)
                    {
                        Console.Write("Automatic identification failed, dropping to search. ");
                    }
                    Console.WriteLine("Please enter search term:");
                    var searchString = Console.ReadLine();
                    var newUri = string.Format("https://www.omdbapi.com/?apikey={0}&s={1}",
                        ripprOptions.OmdbApiKey, searchString);
                    var listOfMovies = await searchDiscDetailsByName(newUri);
                    string selection;
                    if (listOfMovies.Count == 1)
                    {
                        selection = "0";
                    }
                    else
                    {
                        for (var i = 0; i < listOfMovies.Count - 1; i++)
                        {
                            dynamic movie = listOfMovies[i];
                            Console.WriteLine($"[{i}] {movie.Type}: {movie.Title} - {movie.Year}");
                        }

                        selection = Console.ReadLine();
                    }

                    int selectionNum;
                    if (int.TryParse(selection, out selectionNum))
                    {
                        dynamic selectedItemId = listOfMovies[selectionNum];
                        var imdbId = selectedItemId.imdbID;
                        var idUri = string.Format("https://www.omdbapi.com/?apikey={0}&i={1}",
                            ripprOptions.OmdbApiKey, imdbId);
                        var searchResponse = await getDiscDetailsById(idUri);
                        mediaInfo.Title = searchResponse.Title;
                        mediaInfo.Year = searchResponse.Year;
                        mediaInfo.Type = searchResponse.Type;
                        mediaInfo.Runtime = searchResponse.Runtime;
                    }
                }

                return mediaInfo;
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                return null;
            }

        Console.WriteLine(
            "It's a CD, skipping because ripper for CDs should handle identification from FreeDB");
        mediaInfo.Type = MUSIC_TYPE;
        return mediaInfo;
    }

    private async Task<object> getDiscDetailsById(string idUri)
    {
        return await getDiscDetailsByName(idUri);
    }

    private async Task<List<object>> searchDiscDetailsByName(string searchUri)
    {
        try
        {
            using (var client = new HttpClient())
            {
                using (var response = await client.GetAsync(searchUri))
                {
                    if (response.StatusCode >= HttpStatusCode.OK &&
                        response.StatusCode < HttpStatusCode.BadRequest)
                        using (var content = response.Content)
                        {
                            var contentString = await content.ReadAsStringAsync();
                            var responseOuter = JsonConvert.DeserializeObject<JObject>(contentString);
                            var responseObj = (JArray)responseOuter.GetValue("Search");
                            var returnList = new List<object>();
                            foreach (var token in responseObj) returnList.Add(token);
                            return returnList;
                        }
                }
            }
        }
        catch (Exception ex)
        {
        }

        return null;
    }

    private async Task<object> getDiscDetailsByName(string requestUri)
    {
        try
        {
            using (var client = new HttpClient())
            {
                using (var response = await client.GetAsync(requestUri))
                {
                    if (response.StatusCode >= HttpStatusCode.OK &&
                        response.StatusCode < HttpStatusCode.BadRequest)
                        using (var content = response.Content)
                        {
                            var contentString = await content.ReadAsStringAsync();
                            dynamic responseObj = JsonConvert.DeserializeObject(contentString);
                            return responseObj;
                        }
                }
            }
        }
        catch (Exception ex)
        {
        }

        return null;
    }

    // https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp


    public string GetDiscTypeFromSize(long size)
    {
        if (size > 650000000 && size < 4700000000) return "CD";

        if (size > 4700000000 && size < 1.708e+10) return "DVD";

        if (size > 1.708e+10 && size < 128000000000)
            return "Blu-Ray";

        return "Unknown";
    }
}