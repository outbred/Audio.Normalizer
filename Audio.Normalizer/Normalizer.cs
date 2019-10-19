using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            Wma,
            M4a
        }

        static Normalizer()
        {
            MediaFoundationApi.Startup();
        }

        private static ConcurrentBag<string> _oldFiles = new ConcurrentBag<string>();

        public static void NormalizeFiles(string directory, bool convertToMp3, float volume)
        {
            _oldFiles.Clear();
            Console.WriteLine("Searching for mp3 files...");
            var mp3s = Directory.GetFiles(directory, "*.mp3", SearchOption.AllDirectories);
            Console.WriteLine($"Found {mp3s.Length} mp3 files to normalize...");
            // taglib doesn't work so well in parallel
            //Parallel.ForEach(mp3s, new ParallelOptions { MaxDegreeOfParallelism = 5 },
            //    (mp3, state) => Normalize(mp3, AudioType.Mp3, volume));
            foreach (var mp3 in mp3s)
                Normalize(mp3, AudioType.Mp3, volume);

            Console.WriteLine("\nSearching for wma files...");
            var wmas = Directory.GetFiles(directory, "*.wma", SearchOption.AllDirectories);
            Console.WriteLine($"Found {wmas.Length} wma files to normalize...");
            foreach (var wma in wmas)
                Normalize(wma, convertToMp3 ? AudioType.Mp3 : AudioType.Wma, volume);

            Console.WriteLine("\nSearching for m4a files...");
            var m4as = Directory.GetFiles(directory, "*.m4a", SearchOption.AllDirectories);
            Console.WriteLine($"Found {m4as.Length} m4a files to normalize...");
            foreach (var m4a in m4as)
                Normalize(m4a, convertToMp3 ? AudioType.Mp3 : AudioType.M4a, volume);
        }

        static void Normalize(string file, AudioType type, float volume)
        {
            if(!CheckFile(file, out var oldFile))
                return;

            try
            {
                switch(type)
                {
                    case AudioType.Mp3:
                        EncodeToMp3(oldFile, file, volume);
                        break;
                    case AudioType.Wma:
                        EncodeToWma(oldFile, file, volume);
                        break;
                    case AudioType.M4a:
                        EncodeToAac(oldFile, file, volume);
                        break;
                }
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine($"Unable to normalize {file}.\n{0}", ex.Message);
            }
        }

        private static bool CheckFile(string file, out string oldFile)
        {
            oldFile = "";
            if(!File.Exists(file))
                return false;

            foreach(var extension in new[] { ".mp3", ".m4a", ".wma" })
            {
                var fileName = Path.GetFileNameWithoutExtension(file) + extension;
                var oldFormat = Path.Combine(Path.GetDirectoryName(file), $"{fileName}.old");
                oldFile = Path.Combine(Path.GetDirectoryName(file), $"{fileName}.unnormalized");

                // this file has already been normalized, so skip it
                if((File.Exists(oldFormat) || File.Exists(oldFile)) && File.Exists(file))
                {
                    Console.WriteLine($"Skipping file '{file}'");
                    return false;
                }

                // correct for failed normalization on this file
                if(File.Exists(oldFile) && !File.Exists(file))
                {
                    try
                    {
                        File.Move(oldFile, file);
                    }
                    catch
                    {
                        Console.WriteLine($"Unable to rename file '{oldFile}' to complete normalization. Please move manually and re-run.");
                        return false;
                    }
                }

                if(File.Exists(oldFormat) && !File.Exists(file))
                {
                    try
                    {
                        File.Move(oldFormat, file);
                    }
                    catch
                    {
                        Console.WriteLine($"Unable to rename file '{oldFormat}' to complete normalization. Please move manually and re-run.");
                        return false;
                    }
                }
            }

            oldFile = file + ".unnormalized";
            return true;
        }

        private static void EncodeToMp3(string file, string outPath, float volume)
        {

            TagLib.File tags = null;
            try
            { tags = TagLib.File.Create(outPath); }
            catch { tags = null; }

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

            if(!FindMax(file, out var reader, volume))
                return;

            Console.WriteLine($"Normalizing volume for {outPath}.");
            MediaFoundationEncoder.EncodeToMp3(reader, outPath, (int)(reader.Length/reader.TotalTime.TotalSeconds));

            if(tags != null)
            {
                var newTags = TagLib.File.Create(outPath);
                tags.Tag.CopyTo(newTags.Tag, true);
                newTags.Save();
            }

        }

        private static void EncodeToWma(string file, string outPath, float volume)
        {
            TagLib.File tags = null;
            try
            { tags = TagLib.File.Create(outPath); }
            catch { tags = null; }

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

            outPath = Path.Combine(Path.GetDirectoryName(outPath), Path.GetFileNameWithoutExtension(outPath) + ".wma");

            if(!FindMax(file, out var reader, volume))
                return;

            Console.WriteLine($"Normalizing volume for {outPath}.");
            MediaFoundationEncoder.EncodeToWma(reader, outPath, (int)(reader.Length/reader.TotalTime.TotalSeconds));

            if(tags != null)
            {
                var newTags = TagLib.File.Create(outPath);
                tags.Tag.CopyTo(newTags.Tag, true);
                newTags.Save();
            }
        }

        private static void EncodeToAac(string file, string outPath, float volume)
        {
            TagLib.File tags = null;
            try
            { tags = TagLib.File.Create(outPath); }
            catch { tags = null; }

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

            outPath = Path.Combine(Path.GetDirectoryName(outPath), Path.GetFileNameWithoutExtension(outPath) + ".m4a");

            if(!FindMax(file, out var reader, volume))
                return;

            Console.WriteLine($"Normalizing volume for {outPath}.");
            MediaFoundationEncoder.EncodeToAac(reader, outPath, (int)(reader.Length/reader.TotalTime.TotalSeconds));

            if(tags != null)
            {
                var newTags = TagLib.File.Create(outPath);
                tags.Tag.CopyTo(newTags.Tag, true);
                newTags.Save();
            }
        }

        private static bool FindMax(string file, out AudioFileReader reader, float volume)
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
            reader.Volume = volume / max;
            return true;
        }

        public static void Shutdown()
        {
            MediaFoundationApi.Shutdown();
        }

        public static void Archive(string directory)
        {
            var unnormalized = directory + ".unnormalized";

            // in case this is called on its own
            var files = Directory.GetFiles(directory, "*.unnormalized", SearchOption.AllDirectories).ToList();
            // TEMP
            files = files.Concat(Directory.GetFiles(directory, "*.old", SearchOption.AllDirectories)).ToList();

            foreach(var file in files.Where(File.Exists))
            {
                // file was: z:\music\artist\song.mp3.unnormalized
                // now should be: z:\music.unnormalized\artist\song.mp3
                var modded = file.Replace(".old", "").Replace(".unnormalized", "").Replace(directory, unnormalized);

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(modded));
                    File.Move(file, modded);
                }
                catch
                {
                    Console.WriteLine($"Unable to move {file} to {modded}.");
                }
            }
        }

        public static void Cleanup(string directory)
        {
            var files = Directory.GetFiles(directory, "*.unnormalized", SearchOption.AllDirectories).ToList();
            // TEMP
            files = files.Concat(Directory.GetFiles(directory, "*.old", SearchOption.AllDirectories)).ToList();

            foreach(var file in files.Where(File.Exists))
            {
                try
                {
                    File.Delete(file);
                }
                catch(Exception ex)
                {
                    Console.Error.WriteLine($"Unable to delete {file}.", ex);
                }
            }
        }
    }
}
