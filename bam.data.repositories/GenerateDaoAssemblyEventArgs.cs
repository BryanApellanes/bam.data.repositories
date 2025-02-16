/*
	Copyright © Bryan Apellanes 2015  
*/

namespace Bam.Data.Repositories
{
	public class GenerateDaoAssemblyEventArgs: EventArgs
	{
		public GenerateDaoAssemblyEventArgs(Exception ex)
		{
			this.Exception = ex;
		}
		public GenerateDaoAssemblyEventArgs(GeneratedAssemblyInfo generatedAssemblyInfo)
		{
			this.GeneratedAssemblyInfo = generatedAssemblyInfo;
		}

		public GeneratedAssemblyInfo GeneratedAssemblyInfo { get; set; }
		
		public Exception Exception { get; set; }
	}
}
