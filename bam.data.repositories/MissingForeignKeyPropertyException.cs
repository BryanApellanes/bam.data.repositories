/*
	Copyright © Bryan Apellanes 2015  
*/

using System.Runtime.Serialization;

namespace Bam.Data.Repositories
{
	[Serializable]
	public class MissingForeignKeyPropertyException: Exception 
	{
		public MissingForeignKeyPropertyException() { }
		protected MissingForeignKeyPropertyException(SerializationInfo info, StreamingContext context)
			: base()
		{ }
		public MissingForeignKeyPropertyException(IEnumerable<string> missingProperties)
			: base("Missing Foreign Keys: \r\n\t{0}".Format(missingProperties.ToArray().ToDelimited(s => s, "\r\n\t")))
		{ }
	}
}
