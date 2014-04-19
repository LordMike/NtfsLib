using System;
using System.Collections.Generic;
using System.IO;
using Mono.Options;
using System.Linq;
using RawDiskLib;

namespace NtfsDir
{
    public class Options
    {
        public string ErrorDetails { get; set; }

        public ActionType ActionType { get; set; }
        public SizeUnit SizeUnit { get; set; }

        public bool ShowFileIds { get; set; }
        public bool ShowAllNames { get; set; }
        public bool ShowAllStreams { get; set; }

        public char Drive { get; set; }

        public string PathArgument { get; set; }
        public PathType PathType { get; set; }

        private OptionSet _options;

        public Options()
        {
            _options = new OptionSet();
            _options.Add("dir=", "Work with a directory", s =>
            {
                PathType = PathType.Directory;
                PathArgument = s;
            });

            _options.Add("fileids", "Show MFT File Ids", s => ShowFileIds = true);
            _options.Add("allnames", "Show all alternate names, such as 8.3 names", s => ShowAllNames = true);
            _options.Add("streams", "Show all streams", s => ShowAllStreams = true);

            _options.Add("bytes", "Print all sizes in Bytes", s => { SizeUnit = SizeUnit.Bytes; });
            _options.Add("sectors", "Print all sizes in Sectors", s => { SizeUnit = SizeUnit.Sectors; });
            _options.Add("clusters", "Print all sizes in Clusters", s => { SizeUnit = SizeUnit.Clusters; });

            _options.Add("full", "Display full details", s => ActionType = ActionType.ShowFull);
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

            List<string> extras;
            try
            {
                extras = _options.Parse(args);
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
                if (!extras.Any())
                {
                    ErrorDetails = "Must specify a directory";
                    return false;
                }

                PathArgument = extras.First();
                extras.RemoveAt(0);

                PathType = PathType.Directory;
            }

            if (ActionType == ActionType.Unknown)
            {
                ActionType = ActionType.ShowFull;
            }

            if (ShowAllNames && ShowAllStreams)
            {
                ErrorDetails = "--allnames and --streams cannot be used together.";
                return false;
            }

            // Cleaning
            if (PathType == PathType.Directory)
            {
                Console.WriteLine(PathArgument);
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

            char[] volumes = Utils.GetAllAvailableVolumes();

            if (!volumes.Contains(Drive))
            {
                ErrorDetails = "The volume " + Drive + " does not exist.";
                return false;
            }

            return true;
        }

        public void DisplayHelp()
        {
            Console.WriteLine("NtfsDir Help");
            _options.WriteOptionDescriptions(Console.Out);
        }
    }

    public enum ActionType
    {
        Unknown,
        ShowHelp,
        ShowFull
    }

    public enum PathType
    {
        Unknown,
        Directory
    }

    public enum SizeUnit
    {
        Clusters,
        Sectors,
        Bytes
    }
}