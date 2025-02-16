/*
	Copyright © Bryan Apellanes 2015  
*/

using System.Runtime.Serialization;
using System.Text;

namespace Bam.Data.Repositories
{
    [Serializable]
	public class DaoGenerationException: Exception
	{
        protected DaoGenerationException(SerializationInfo info, StreamingContext context) { }
		public DaoGenerationException(string schemaName, string schemaHash, Type[] types, Exception compilationEx) : base(GetMessage(schemaName, schemaHash, types, compilationEx)) { }

        private static string GetMessage(string schemaName, string schemaHash, Type[] types, Exception compilationEx)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendFormat("Unable to Generate Dao Assembly for {0} ({1}).\r\nSpecified Types:\r\n", schemaName, schemaHash);
			foreach(Type type in types)
			{
				builder.AppendFormat("\t{0}\r\n", type.FullName);
			}

			builder.AppendLine();
			builder.AppendFormat("\t{0}", compilationEx?.Message ?? "[CompilationEx is null]");

			return builder.ToString();
		}
	}
}
