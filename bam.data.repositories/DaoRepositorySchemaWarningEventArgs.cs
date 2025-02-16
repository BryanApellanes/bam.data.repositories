/*
	Copyright © Bryan Apellanes 2015  
*/

namespace Bam.Data.Repositories
{
	public class DaoRepositorySchemaWarningEventArgs: EventArgs
	{
		public DaoRepositorySchemaWarningEventArgs() { }
		public string ClassName { get; set; }
		public string PropertyName { get; set; }
		public string PropertyType { get; set; }
	}
}
