using System;
using System.Collections.Generic;
using System.Text;

namespace ManagedFusion.Crawler
{
	[Flags]
	public enum RobotTag
	{
		Null				= 0x01,
		Index				= 0x02,
		NoIndex				= 0x04,
		Follow				= 0x08,
		NoFollow			= 0x10,
		NoArchive			= 0x20,
		NoSnippet			= 0x40,
		UnavailableAfter	= 0x80
	}
}
