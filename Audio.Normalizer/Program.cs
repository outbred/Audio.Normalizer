using System;

namespace Audio.Normalizer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                Console.WriteLine("usage: normalizer.exe <directory> </keep>\nkeep - keeps files in original format; if omitted, then all files are converted to MP3 format.");
                return;
            }

            var directory = args[0];
            bool convertToMp3 = true;
            if (args.Length == 2)
                convertToMp3 = args[1].ToUpper() == "/KEEP";
            Normalizer.NormalizeFiles(directory, convertToMp3);
            Console.WriteLine("Normalization completed.  Deleted old files? (Y|N)");
            var answer = Console.ReadKey();
            if (answer.KeyChar == 'Y' || answer.KeyChar == 'y')
                Normalizer.CleanUp();

            Normalizer.Shutdown();
            Console.WriteLine("");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
