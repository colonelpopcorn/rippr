using Mono.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rippr
{
    class Program
    {
        static void Main(string[] args)
        {
            RipprOptions ripprOptions = RipprOptions.getDefault();
            var p = new OptionSet() {
                {
                  "omdb-api-key=",
                  "An {APIKEY} to search for movies on OMDB.",
                  v => ripprOptions.OmdbApiKey = v
                },
                {
                  "cd-input-path=",
                  "Temp {DIRECTORY} to place CD rips.",
                  v => ripprOptions.CDPathInfo.InputPath = v
                },
                {
                  "dvd-input-path=",
                  "Temp {DIRECTORY} to place DVD rips.",
                  v => ripprOptions.DVDPathInfo.InputPath = v
                },
                {
                  "blu-ray-input-path=",
                  "Temp {DIRECTORY} to place Blu-Ray rips.",
                  v => ripprOptions.BluRayPathInfo.InputPath = v
                },
                {
                  "cd-output-path=",
                  "Post rip {DIRECTORY} for CD rips.",
                  v => ripprOptions.CDPathInfo.OutputPath = v
                },
                {
                  "dvd-output-path=",
                  "Post rip {DIRECTORY} for DVD rips.",
                  v => ripprOptions.DVDPathInfo.OutputPath = v
                },
                {
                  "blu-ray-output-path=",
                  "Post rip {DIRECTORY} for Blu-Ray rips.",
                  v => ripprOptions.BluRayPathInfo.OutputPath = v
                },
                {
                  "cd-ripper-path=",
                  "{EXEPATH} for CD ripping.",
                  v => ripprOptions.CDPathInfo.RipperExePath = v
                },
                {
                  "dvd-ripper-path=",
                  "{EXEPATH} for DVD ripping.",
                  v => ripprOptions.DVDPathInfo.RipperExePath = v
                },
                {
                  "blu-ray-ripper-path=",
                  "{EXEPATH} for Blu-Ray ripping.",
                  v => ripprOptions.BluRayPathInfo.RipperExePath = v
                },
                {
                  "cd-ripper-args=",
                  "{ARGSFMTSTR} for CD ripping executable.",
                  v => ripprOptions.CDPathInfo.RipperExeOpts = v
                },
                {
                  "dvd-ripper-args=",
                  "{ARGSFMTSTR} for DVD ripping executable.",
                  v => ripprOptions.DVDPathInfo.RipperExeOpts = v
                },
                {
                  "blu-ray-ripper-args=",
                  "{ARGSFMTSTR} for Blu-Ray ripping executable.",
                  v => ripprOptions.BluRayPathInfo.RipperExeOpts = v
                },
                {
                  "batch",
                  "Run Rippr in batch mode.",
                  v => ripprOptions.IsBatchMode = v != null
                },
                {
                  "config-path",
                  "Path to a custom configuration.",
                  argConfigPath => ripprOptions = getOptionsFromConfigPath(argConfigPath)
                },
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
                List<string> extra = p.Parse(args);
                if (ripprOptions.ShouldShowHelp)
                {
                    ShowHelp(p);
                }
                else
                {
                    do
                    {
                        DoRip(ripprOptions).Wait();
                    }
                    while (ripprOptions.IsBatchMode);
                }
            }
            catch (OptionException e)
            {
                Console.Write("greet: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `greet --help' for more information.");
                return;
            }
        }

        private static RipprOptions getOptionsFromConfigPath(string argConfigPath)
        {
            throw new NotImplementedException();
        }

        static async Task DoRip(RipprOptions ripprOptions)
        {
            Console.WriteLine("Identifying disc(s)...");
            var service = new RippingService(ripprOptions);

            var discInfo = await service.GetDiscInfoList();

            Console.WriteLine("Identification done, starting rip...");

            service.RipEachDisc(discInfo);

            Console.WriteLine("Ripping is done, starting move...");

            service.MoveToOutputDir(discInfo);

        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: rippr [OPTIONS]");
            Console.WriteLine("Automatically rip optical discs.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }
    }
}