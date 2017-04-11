using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Mono.Options;
using System.Linq;
using NTFSLib.Objects.Enums;
using RawDiskLib;

namespace NtfsCopy
{
    public class Options
    {
        public string ErrorDetails { get; set; }

        public char Drive { get; set; }

        public ActionType ActionType { get; set; }

        public uint MftId { get; set; }
        public string Source { get; set; }
        public AttributeType SourceAttribute { get; set; }
        public string SourceName { get; set; }

        public PathType SourceType { get; set; }

        public string Destination { get; set; }

        private OptionSet _options;

        public Options()
        {
            SourceAttribute = AttributeType.DATA;
            SourceName = string.Empty;

            _options = new OptionSet();
            _options.Add("file=", "Source file", s =>
                {
                    SourceType = PathType.File;
                    Source = s;
                });
            _options.Add("attribute=", "Source Attribute Type", s =>
                {
                    if (Enum.IsDefined(typeof(AttributeType), s))
                        SourceAttribute = (AttributeType)Enum.Parse(typeof(AttributeType), s);
                });
            _options.Add("attributename=", "Source Attribute Name", s =>
                {
                    SourceName = s;
                });

            _options.Add("volume=", "Source volume", s =>
            {
                if ((s.Length == 1 && char.IsLetter(s[0])) || (s.Length == 2 && char.IsLetter(s[0]) && s[2] == ':'))
                    Drive = s[0];
            });
            _options.Add("mftid=", "Source MFT Id", s =>
            {
                uint x;
                if (uint.TryParse(s, out x))
                {
                    SourceType = PathType.MftId;
                    MftId = x;
                }
            });

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
            if (SourceType == PathType.Unknown)
            {
                if (extras.Any())
                {
                    // Use extras[0] as source
                    Source = extras.First();
                    SourceType = PathType.File;

                    extras.RemoveAt(0);
                }
                else
                {
                    ErrorDetails = "Must specify a source";
                    return false;
                }
            }

            if (!char.IsLetter(Drive) && SourceType == PathType.MftId)
            {
                ErrorDetails = "Must specify --volume when using for --mftid";
                return false;
            }

            // Destination
            if (extras.Any())
            {
                Destination = extras.First();
                extras.RemoveAt(0);

                if (!Path.IsPathRooted(Destination))
                    Destination = Path.Combine(Environment.CurrentDirectory, Destination);

                string destinationDir = Path.GetDirectoryName(Destination);
                if (!Directory.Exists(destinationDir))
                {
                    ErrorDetails = "Destination directory, " + destinationDir + " does not exist.";
                    return false;
                }
            }
            else
            {
                ErrorDetails = "Must specify a destination";
                return false;
            }

            // Validation
            if (Destination.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                ErrorDetails = "Destination has invalid characters: " + Destination[Destination.IndexOfAny(Path.GetInvalidPathChars())];
                return false;
            }

            if (Path.GetFileName(Destination).IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                ErrorDetails = "Destination filename has invalid characters: " + Path.GetFileName(Destination)[Path.GetFileName(Destination).IndexOfAny(Path.GetInvalidFileNameChars())];
                return false;
            }

            // Cleaning
            if (SourceType == PathType.File)
            {
                if ((Source[0] == '"' || Source[0] == '\'') && Source[0] == Source.Last())
                {
                    // Strip quotes
                    Source = Source.Substring(1, Source.Length - 2);
                }

                // Fixup
                if (!Path.IsPathRooted(Source))
                {
                    Source = Path.Combine(Environment.CurrentDirectory, Source);
                }
            }

            if (!char.IsLetter(Drive))
            {
                Drive = Source[0];
            }

            char[] volumes = Utils.GetAllAvailableVolumes().ToArray();

            if (!volumes.Contains(Drive))
            {
                ErrorDetails = "The volume " + Drive + " does not exist.";
                return false;
            }

            // Parse attribute name and type
            if (Source.IndexOf(':', 3) != -1)
            {
                // Has an attribute type and possibly a name
                string attr = Source.Substring(Source.IndexOf(':', 3));
                string[] attrs = attr.Split(':');

                if (attrs.Length != 3)
                {
                    ErrorDetails = "Invalid format for attribute specification. Must be in the form: filename:ATTRNAME:$TYPE.";
                    return false;
                }

                SourceName = attrs[1];

                if (attrs[2].Length <= 2)
                {
                    ErrorDetails = "Please specify a valid attribute type";
                    return false;
                }

                if (attrs[2][0] != '$')
                {
                    ErrorDetails = "Please specify a valid attribute type";
                    return false;
                }

                if (Enum.IsDefined(typeof(AttributeType), attrs[2].Substring(1)))
                    SourceAttribute = (AttributeType)Enum.Parse(typeof(AttributeType), attrs[2].Substring(1));

                Source = Source.Substring(0, Source.IndexOf(':', 3));
            }

            return true;
        }

        public void DisplayHelp()
        {
            Console.WriteLine("NtfsCopy Help");
            Console.WriteLine("NtfsCopy <SOURCE> <DESTINATION>");
            Console.WriteLine("NtfsCopy --volume C --mftid 0 <DESTINATION>");
            Console.WriteLine("NtfsCopy C:\\$MFT::$BITMAP <DESTINATION>");
            Console.WriteLine("NtfsCopy C:\\$MFT --attribute BITMAP <DESTINATION>");
            Console.WriteLine();
            _options.WriteOptionDescriptions(Console.Out);
        }
    }

    public enum ActionType
    {
        Copy,
        ShowHelp
    }

    public enum PathType
    {
        Unknown,
        File,
        MftId
    }
}