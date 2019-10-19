using System;
using System.Collections.Generic;
using System.Text;
using Ookii.CommandLine;

namespace Audio.Normalizer
{
    public class Options
    {
        [CommandLineArgument(DefaultValue = null, IsRequired = true, Position = 0, ValueDescription = "Directory to recursively search for audio files. Mp3, m4a, and wma files on Windows are currently supported.")]
        public string directory { get; set; }

        [CommandLineArgument(DefaultValue = true, IsRequired = false, ValueDescription = "Normalizes audio files for the given directory.")]
        public bool normalize { get; set; }

        [CommandLineArgument(DefaultValue = false, IsRequired = false, ValueDescription = "Keep files in original format? False converts all to mp3.")]
        public bool keep {get; set; }

        [CommandLineArgument(DefaultValue = false, IsRequired = false, ValueDescription = "Performed only if directory is set. Deletes files with '.unnormalized' extension. Cleanup or archive should be set, but not both.")]
        public bool cleanup { get; set; }

        [CommandLineArgument(DefaultValue = false, IsRequired = false, ValueDescription = "Performed only if directory is set. Archives unnormalized files (with extension '.unnormalized') into a co-located folder with '.unnormalized' appended to it. Cleanup or archive can be set, but not both.")]
        public bool archive { get; set; }

        [CommandLineArgument(DefaultValue = 1.0f, IsRequired = false, ValueDescription = "Desired volume. 0 < volume <= 1")]
        public float volume { get; set; }
    }
}
