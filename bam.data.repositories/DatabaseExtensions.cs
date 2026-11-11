namespace Bam.Data.Repositories
{
    public static class DatabaseExtensions
    {
        public static SqlStringBuilder WriteSchemaScript(this IDatabase db, DaoSchemaDefinitionCreateResult schemaInfo)
        {
            TypeSchemaScriptWriter writer = new TypeSchemaScriptWriter();
            return writer.WriteSchemaScript(db, schemaInfo);
        }

        /// <summary>
        /// Writes the schema script for <paramref name="schemaInfo"/> and executes it as a schema script, so tables that
        /// already exist don't stop the ones that don't from being created.
        /// </summary>
        /// <param name="db">The database to create the schema in.</param>
        /// <param name="schemaInfo">The schema definition to write.</param>
        public static void CommitSchema(this IDatabase db, DaoSchemaDefinitionCreateResult schemaInfo)
        {
            db.ExecuteSchemaScript(WriteSchemaScript(db, schemaInfo));
        }
    }
}
