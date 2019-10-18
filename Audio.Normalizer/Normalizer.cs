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

            // convert over m4as to mp3s too
            Console.WriteLine("Searching for m4a files...");
            var m4as = Directory.GetFiles(directory, "*.m4a", SearchOption.AllDirectories);
            Console.WriteLine($"Found {wmas.Length} m4a files to normalize...");
            Parallel.ForEach(m4as, new ParallelOptions { MaxDegreeOfParallelism = 5 },
                (m4a, state) => Normalize(m4a, convertToMp3 ? AudioType.Mp3 : AudioType.M4a));
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
                case AudioType.M4a:
                    EncodeToAac(oldFile, file);
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

            //using (var reader = new MediaFoundationReader(InputFile))
            //{
            //var OutputFormats = new Dictionary<string, Guid>();
            //OutputFormats.Add("AAC", AudioSubtypes.MFAudioFormat_AAC); // Windows 8 can do a .aac extension as well
            //OutputFormats.Add("Windows Media Audio", AudioSubtypes.MFAudioFormat_WMAudioV8);
            //OutputFormats.Add("Windows Media Audio Professional", AudioSubtypes.MFAudioFormat_WMAudioV9 );
            //OutputFormats.Add("MP3", AudioSubtypes.MFAudioFormat_MP3);
            //OutputFormats.Add("Windows Media Audio Voice", AudioSubtypes.MFAudioFormat_MSP1);
            //OutputFormats.Add("Windows Media Audio Lossless", AudioSubtypes.MFAudioFormat_WMAudio_Lossless);
            //OutputFormats.Add("FLAC", Guid.Parse("0000f1ac-0000-0010-8000-00aa00389b71"));
            //OutputFormats.Add("Apple Lossless (ALAC)", Guid.Parse("63616c61-0000-0010-8000-00aa00389b71"));

            //using(var encoder = new MediaFoundationEncoder(MediaFoundationEncoder.GetOutputMediaTypes(AudioSubtypes.MFAudioFormat_MP3).FirstOrDefault()))
            //{
            //    encoder.Encode(outPath, reader);
            //}

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

            outPath = Path.Combine(Path.GetDirectoryName(outPath), Path.GetFileNameWithoutExtension(outPath) + ".wma");

            if(!FindMax(file, out var reader))
                return;

            Console.WriteLine($"Normalizing volume for {outPath}.");
            MediaFoundationEncoder.EncodeToWma(reader, outPath, 320000);

            var newTags = TagLib.File.Create(outPath);
            tags.Tag.CopyTo(newTags.Tag, true);
            newTags.Save();
        }

        private static void EncodeToAac(string file, string outPath)
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

            outPath = Path.Combine(Path.GetDirectoryName(outPath), Path.GetFileNameWithoutExtension(outPath) + ".m4a");

            if(!FindMax(file, out var reader))
                return;

            Console.WriteLine($"Normalizing volume for {outPath}.");
            MediaFoundationEncoder.EncodeToAac(reader, outPath, 320000);

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
