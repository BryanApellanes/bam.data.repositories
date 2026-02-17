/*
	Copyright © Bryan Apellanes 2015  
*/

using System.Reflection;

namespace Bam.Data.Repositories
{
	public class MetaPropertyVersionInfo
	{
		public MetaPropertyVersionInfo() { }

		public string Hash { get; internal set; } = null!;
		public PropertyInfo PropertyInfo { get; internal set; } = null!;
		public DateTime LastWrite { get; internal set; }
		public string Name { get; internal set; } = null!;
		public int Version { get; internal set; }
		public object Value { get; internal set; } = null!;
	}
}
