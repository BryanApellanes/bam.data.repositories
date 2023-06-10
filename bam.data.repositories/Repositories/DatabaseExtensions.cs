using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Net.Data.Repositories
{
    public static class DatabaseExtensions
    {
        public static SqlStringBuilder WriteSchemaScript(this IDatabase db, SchemaDefinitionCreateResult schemaInfo)
        {
            TypeSchemaScriptWriter writer = new TypeSchemaScriptWriter();
            return writer.WriteSchemaScript(db, schemaInfo);
        }

        public static void CommitSchema(this IDatabase db, SchemaDefinitionCreateResult schemaInfo)
        {
            db.ExecuteSql((ISqlStringBuilder)WriteSchemaScript(db, schemaInfo));
        }
    }
}
