# NTFSLib
NTFS Library to parse NTFS structures and filesystems. I primarily made this project to better understand how filesystems work and to be able to make better judgements when using filesystems in everyday life. It dawned on me that one of the most-used filesystems, NTFS, had no C# parser (I later discovered [DiscUtils](https://discutils.codeplex.com/)).

This library mainly focuses on parsing the MFT (master file table) and as such it ignores journaling, security descriptors and other similar topics. My main goal was to be able to parse a filesystem in such a way that I could read it - and by far the MFT is he most important part of NTFS.

I did this project some two years ago, but I never got around to opensourcing it. I now have.
Please note that this project is provided as-is, and is not subject to any form of support. I find the project educational, and have used it from time to time to diagnose issues with NTFS filesystems or copy out protected files (NtfsDetails and NtfsCopy) - but I have also discovered issues with some filesystems.

# License
This project is under the MIT license. As such you are allowed to modify and use the code, but are required to attribute me when using it.

# Structure
All NTFS code resides in the NTFSLib project, which houses both parsing and the structures themselves. This library also holds two wrappers around the NTFS filesystem to help users get started, namely NTFSParser (sequential filerecord parsing) and NTFSWrapper (more direct file and directory access).

The documentation is largely not written, but a series of utility programs exist to help users get started. Namely:

- NtfsDetails
- NtfsCopy
- NtfsDir
- NtfsExtract

NtfsLib depends on two other projects which I also made myself. 

### RawDiskLib
[RawDiskLib](https://github.com/LordMike/RawDiskLib) is a library that facilitates reading to and from raw devices using classic Win32 API's. It mainly alleviates the trouble of reading in sector-sized chunks.

### DeviceIOControlLib
[DeviceIOControlLib](https://github.com/LordMike/DeviceIOControlLib) is used by RawDiskLib to find out the sector size. This could probably be inlined.

# Research and attribution
I've largely discovered the details about NTFS that I needed to complete this project from a few sources. Other sources, which I've since forgotten, have most surely been found by Googling and stumbling across forensics PDF's and Powerpoints.

**NTFS.com** has some good starting information and basic gotchas. This site got me started and will most likely be what you stumble across all the time when searching for NTFS-related technicalities. http://ntfs.com/

**MSDN** contains a lot of information on the individual structures of NTFS. A good starting point [is here](https://msdn.microsoft.com/en-us/library/bb470206%28v=vs.85%29.aspx).

**R-TT's R-Studio** recovery tool. [This tool](http://www.r-studio.com/) allows you to view the disk with a hex editor, and overlays templates containing file structures (much like some other fancy hex editors today). I found R-Studios templates to be very informing (and sometimes incorrect). Having a good hex editor / viewer is crucial when parsing binary data structures. 

**Other sources** [Wikipedia](https://en.wikipedia.org/wiki/NTFS), various [Google searches](https://www.google.dk/webhp?sourceid=chrome-instant&ion=1&espv=2&ie=UTF-8#safe=off&q=ntfs+forensics) .. and many more (remember, this was two years ago :)).
