using System;
using System.Globalization;
using System.IO;
using Ookii.CommandLine;

namespace Audio.Normalizer
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new CommandLineParser(typeof(Options), new []{ "/"});
            Options options = null;
            try
            {
                options = (Options)parser.Parse(args);
                if(string.IsNullOrWhiteSpace(options.directory) || !Directory.Exists(options.directory))
                    throw new ArgumentNullException(nameof(options.directory));
            }
            catch//( CommandLineArgumentException ex )
            {
                //Console.WriteLine(ex.Message);
                parser.WriteUsageToConsole();
                return;
            }


            if (options.normalize)
            {
                Normalizer.NormalizeFiles(options.directory, !options.keep, options.volume);
                Console.WriteLine("Normalization completed\n.");
            }

            if(options.archive)
                Normalizer.Archive(options.directory);

            if (options.cleanup)
                Normalizer.Cleanup(options.directory);

            Normalizer.Shutdown();
            Console.WriteLine("");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
