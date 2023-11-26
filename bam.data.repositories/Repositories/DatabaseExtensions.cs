using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Net.Data.Repositories
{
    public static class DatabaseExtensions
    {
        public static SqlStringBuilder WriteSchemaScript(this IDatabase db, DaoSchemaDefinitionCreateResult schemaInfo)
        {
            TypeSchemaScriptWriter writer = new TypeSchemaScriptWriter();
            return writer.WriteSchemaScript(db, schemaInfo);
        }

        public static void CommitSchema(this IDatabase db, DaoSchemaDefinitionCreateResult schemaInfo)
        {
            db.ExecuteSql((ISqlStringBuilder)WriteSchemaScript(db, schemaInfo));
        }
    }
}
