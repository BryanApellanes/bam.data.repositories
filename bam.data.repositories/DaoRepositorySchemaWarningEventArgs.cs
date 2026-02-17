/*
	Copyright © Bryan Apellanes 2015  
*/

namespace Bam.Data.Repositories
{
	public class DaoRepositorySchemaWarningEventArgs: EventArgs
	{
		public DaoRepositorySchemaWarningEventArgs() { }
		public string ClassName { get; set; } = null!;
		public string PropertyName { get; set; } = null!;
		public string PropertyType { get; set; } = null!;
	}
}
