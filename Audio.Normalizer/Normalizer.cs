using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.WindowsMediaFormat;

namespace Audio.Normalizer
{
    public class Normalizer
    {
        private enum AudioType
        {
            Mp3,
            Wma
        }

        static Normalizer()
        {
            MediaFoundationApi.Startup();
        }

        private static readonly ConcurrentBag<string> _oldFiles = new ConcurrentBag<string>();

        public static void NormalizeFiles(string directory, bool convertToMp3)
        {
            _oldFiles.Clear();
            Console.WriteLine("Searching for mp3 files...");
            var mp3s = Directory.GetFiles(directory, "*.mp3", SearchOption.AllDirectories);
            Console.WriteLine($"Found {mp3s.Length} mp3 files to normalize...");
            Parallel.ForEach(mp3s, new ParallelOptions { MaxDegreeOfParallelism = 5 },
                (mp3, state) => Normalize(mp3, AudioType.Mp3));

            // convert over wmas to mp3s too
            Console.WriteLine("Searching for wma files...");
            var wmas = Directory.GetFiles(directory, "*.wma", SearchOption.AllDirectories);
            Console.WriteLine($"Found {wmas.Length} wma files to normalize...");
            Parallel.ForEach(wmas, new ParallelOptions { MaxDegreeOfParallelism = 5 },
                (wma, state) => Normalize(wma, convertToMp3 ? AudioType.Mp3 : AudioType.Wma));
        }

        static void Normalize(string file, AudioType type)
        {
            if(!File.Exists(file))
                return;

            var oldFile = Path.Combine(Path.GetDirectoryName(file), $"{Path.GetFileName(file)}.old");

            // this file has already been normalized, so skip it
            if (File.Exists(oldFile) && File.Exists(file))
            {
                Console.WriteLine($"Skipping file '{file}'");
                return;
            }

            // correct for failed normalization on this file
            if (File.Exists(oldFile) && !File.Exists(file))
            {
                try { File.Move(oldFile, file); }
                catch
                {
                    Console.WriteLine($"Unable to rename file '{oldFile}' to complete normalization. Please move manually and re-run.");
                    return;
                }
            }

            switch(type)
            {
                case AudioType.Mp3:
                    EncodeToMp3(oldFile, file);
                    break;
                case AudioType.Wma:
                    EncodeToWma(oldFile, file);
                    break;
            }
        }

        private static void EncodeToMp3(string file, string outPath)
        {
            var tags = TagLib.File.Create(outPath);

            try
            {
                File.Move(outPath, file);
                _oldFiles.Add(file);
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine($"Unable to move file {outPath}", ex);
                return;
            }

            outPath = Path.Combine(Path.GetDirectoryName(outPath), Path.GetFileNameWithoutExtension(outPath) + ".mp3");

            if(!FindMax(file, out var reader))
                return;

            Console.WriteLine($"Normalizing volume for {outPath}.");
            MediaFoundationEncoder.EncodeToMp3(reader, outPath, 320000);

            var newTags = TagLib.File.Create(outPath);
            tags.Tag.CopyTo(newTags.Tag, true);
            newTags.Save();

        }

        private static void EncodeToWma(string file, string outPath)
        {
            var tags = TagLib.File.Create(outPath);

            try
            {
                File.Move(outPath, file);
                _oldFiles.Add(file);
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine($"Unable to move file {outPath}", ex);
                return;
            }

            outPath = Path.Combine(Path.GetDirectoryName(outPath), Path.GetFileNameWithoutExtension(outPath) + ".mp3");

            if(!FindMax(file, out var reader))
                return;

            Console.WriteLine($"Normalizing volume for {outPath}.");
            MediaFoundationEncoder.EncodeToWma(reader, outPath, 320000);

            var newTags = TagLib.File.Create(outPath);
            tags.Tag.CopyTo(newTags.Tag, true);
            newTags.Save();
        }

        private static bool FindMax(string file, out AudioFileReader reader)
        {
            float max = 0;

            reader = new AudioFileReader(file);
            // find the max peak
            float[] buffer = new float[reader.WaveFormat.SampleRate];
            int read;
            do
            {
                read = reader.Read(buffer, 0, buffer.Length);
                for(int n = 0; n < read; n++)
                {
                    var abs = Math.Abs(buffer[n]);
                    if(abs > max)
                        max = abs;
                }
            } while(read > 0);

            //Console.WriteLine($"Max sample value: {max}");

            if(max == 0 || max > 1.0f)
                return false;

            // rewind and amplify
            reader.Position = 0;
            reader.Volume = 1.0f / max;
            return true;
        }

        public static void Shutdown()
        {
            MediaFoundationApi.Shutdown();
        }

        public static void CleanUp()
        {
            foreach (var file in _oldFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Unable to delete {file}.", ex);
                }
            }
        }
    }
}
