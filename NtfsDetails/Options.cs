using System;
using System.IO;
using Mono.Options;
using System.Linq;
using RawDiskLib;

namespace NtfsDetails
{
    public class Options
    {
        public string ErrorDetails { get; set; }

        public ActionType ActionType { get; set; }
        public SizeUnit SizeUnit { get; set; }

        public char Drive { get; set; }

        public uint MftId { get; set; }
        public string PathArgument { get; set; }
        public PathType PathType { get; set; }

        private OptionSet _options;

        public Options()
        {
            _options = new OptionSet();
            _options.Add("file=", "Work with a file", s =>
                {
                    PathType = PathType.File;
                    PathArgument = s;
                });
            _options.Add("dir=", "Work with a directory", s =>
            {
                PathType = PathType.Directory;
                PathArgument = s;
            });
            _options.Add("volume=", "Work with a volume", s =>
            {
                if ((s.Length == 1 && char.IsLetter(s[0])) || (s.Length == 2 && char.IsLetter(s[0]) && s[2] == ':'))
                    Drive = s[0];
            });

            _options.Add("mftid=", "Specify an MFT Id directly", s =>
            {
                uint x;
                if (uint.TryParse(s, out x))
                {
                    PathType = PathType.MftId;
                    MftId = x;
                }
            });

            _options.Add("bytes", "Print all sizes in Bytes", s => { SizeUnit = SizeUnit.Bytes; });
            _options.Add("sectors", "Print all sizes in Sectors", s => { SizeUnit = SizeUnit.Sectors; });
            _options.Add("clusters", "Print all sizes in Clusters", s => { SizeUnit = SizeUnit.Clusters; });

            _options.Add("full", "Display full details", s => ActionType = ActionType.ShowFull);
            _options.Add("extents", "Display all extents", s => ActionType = ActionType.ShowExtents);
            _options.Add("paths", "Display all paths to this file/dir", s => ActionType = ActionType.Path);
            _options.Add("help", "Display a help page", s => ActionType = ActionType.ShowHelp);

            ErrorDetails = null;
        }

        public bool Parse(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                ActionType = ActionType.ShowHelp;
                return true;
            }

            try
            {
                _options.Parse(args);
            }
            catch (Exception ex)
            {
                ErrorDetails = ex.Message;
                return false;
            }

            if (ActionType == ActionType.ShowHelp)
                return true;

            // Validation
            if (PathType == PathType.Unknown)
            {
                ErrorDetails = "Must specify either a --file, --dir or an --mftid";
                return false;
            }

            if (ActionType == ActionType.Unknown)
            {
                ErrorDetails = "Must specify an action, such as --extents, --full or --paths";
                return false;
            }

            if (ActionType != ActionType.Unknown && PathType == PathType.Unknown)
            {
                ErrorDetails = "Must specify either a --file, --dir or --mftid for --extents, --full or --paths";
                return false;
            }

            if (!char.IsLetter(Drive) && PathType == PathType.MftId)
            {
                ErrorDetails = "Must specify --volume when using for --mftid";
                return false;
            }

            // Cleaning
            if (PathType == PathType.File || PathType == PathType.Directory)
            {
                if ((PathArgument[0] == '"' || PathArgument[0] == '\'') && PathArgument[0] == PathArgument.Last())
                {
                    // Strip quotes
                    PathArgument = PathArgument.Substring(1, PathArgument.Length - 2);
                }

                // Fixup
                if (!Path.IsPathRooted(PathArgument))
                {
                    PathArgument = Path.Combine(Environment.CurrentDirectory, PathArgument);
                }
            }

            if (!char.IsLetter(Drive))
            {
                Drive = PathArgument[0];
            }

            char[] volumes = Utils.GetAllAvailableVolumes().ToArray();

            if (!volumes.Contains(Drive))
            {
                ErrorDetails = "The volume " + Drive + " does not exist.";
                return false;
            }

            return true;
        }

        public void DisplayHelp()
        {
            Console.WriteLine("NtfsDetails Help");
            _options.WriteOptionDescriptions(Console.Out);
        }
    }

    public enum ActionType
    {
        Unknown,
        ShowHelp,
        ShowFull,
        ShowExtents,
        Path
    }

    public enum PathType
    {
        Unknown,
        File,
        Directory,
        MftId
    }

    public enum SizeUnit
    {
        Clusters,
        Sectors,
        Bytes
    }
}