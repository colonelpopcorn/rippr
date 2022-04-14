using System;
using System.Threading.Tasks;
using Mono.Options;

namespace Rippr;

internal class Program
{
    private static void Main(string[] args)
    {
        var ripprOptions = RipprOptions.getDefault();
        var argConfigPath = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
        var p = new OptionSet
        {
            {
                "omdb-api-key=",
                "An {APIKEY} to search for movies on OMDB.",
                v => ripprOptions.OmdbApiKey = v
            },
            {
                "cd-input-path=",
                "Temp {DIRECTORY} to place CD rips.",
                v => ripprOptions.CdInputOpts.InputPath = v
            },
            {
                "dvd-input-path=",
                "Temp {DIRECTORY} to place DVD rips.",
                v => ripprOptions.DvdInputOpts.InputPath = v
            },
            {
                "blu-ray-input-path=",
                "Temp {DIRECTORY} to place Blu-Ray rips.",
                v => ripprOptions.BluRayInputOpts.InputPath = v
            },
            {
                "music-output-path=",
                "Post rip {DIRECTORY} for music rips.",
                v => ripprOptions.OutputOpts.MusicOutputPath = v
            },
            {
                "movie-output-path=",
                "Post rip {DIRECTORY} for movie rips.",
                v => ripprOptions.OutputOpts.MovieOutputPath = v
            },
            {
                "tv-output-path=",
                "Post rip {DIRECTORY} for tv rips.",
                v => ripprOptions.OutputOpts.TVOutputPath = v
            },
            {
                "iso-output-path=",
                "Post rip {DIRECTORY} for ISO rips.",
                v => ripprOptions.OutputOpts.ISOOutputPath = v
            },
            {
                "cd-ripper-path=",
                "{EXEPATH} for CD ripping.",
                v => ripprOptions.CdInputOpts.RipperExePath = v
            },
            {
                "dvd-ripper-path=",
                "{EXEPATH} for DVD ripping.",
                v => ripprOptions.DvdInputOpts.RipperExePath = v
            },
            {
                "blu-ray-ripper-path=",
                "{EXEPATH} for Blu-Ray ripping.",
                v => ripprOptions.BluRayInputOpts.RipperExePath = v
            },
            {
                "cd-ripper-args=",
                "{ARGSFMTSTR} for CD ripping executable.",
                v => ripprOptions.CdInputOpts.RipperExeOpts = v
            },
            {
                "dvd-ripper-args=",
                "{ARGSFMTSTR} for DVD ripping executable.",
                v => ripprOptions.DvdInputOpts.RipperExeOpts = v
            },
            {
                "blu-ray-ripper-args=",
                "{ARGSFMTSTR} for Blu-Ray ripping executable.",
                v => ripprOptions.BluRayInputOpts.RipperExeOpts = v
            },
            {
                "batch",
                "Run Rippr in batch mode.",
                v => ripprOptions.IsBatchMode = v != null
            },
            // {
            //     "config-path=",
            //     "Path to a custom {CONFIGURATION} file.",
            //     argConfigPath => argConfigPath = argConfigPath
            // },
            {
                "debug",
                "Print everything the app would do if debug flag was not set.",
                v => ripprOptions.IsDebugMode = v != null
            },
            {
                "help",
                "Show options for Rippr.",
                v => ripprOptions.ShouldShowHelp = v != null
            }
        };

        try
        {
            var extra = p.Parse(args);
            if (ripprOptions.ShouldShowHelp)
                ShowHelp(p);
            else
                do
                {
                    DoRip(ripprOptions).Wait();
                } while (ripprOptions.IsBatchMode);
        }
        catch (OptionException e)
        {
            Console.Write("rippr: ");
            Console.WriteLine(e.Message);
            Console.WriteLine("Try `rippr --help' for more information.");
        }
    }

    private static RipprOptions getOptionsFromConfigPath(string argConfigPath)
    {
        throw new NotImplementedException();
    }

    private static async Task DoRip(RipprOptions ripprOptions)
    {
        Console.WriteLine("Identifying disc(s)...");
        var service = new RippingService(ripprOptions);

        var discInfo = await service.GetDiscInfoList();

        Console.WriteLine("Identification done, starting rip...");

        service.RipEachDisc(discInfo);

        Console.WriteLine("Ripping is done, starting move...");

        service.MoveToOutputDir(discInfo);
    }

    private static void ShowHelp(OptionSet p)
    {
        Console.WriteLine("Usage: rippr [OPTIONS]");
        Console.WriteLine("Automatically rip optical discs.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        p.WriteOptionDescriptions(Console.Out);
    }
}