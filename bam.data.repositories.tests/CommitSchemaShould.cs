using Bam.Data.SQLite;
using Bam.DependencyInjection;
using Bam.Test;

namespace Bam.Data.Repositories.Tests
{
    /// <summary>
    /// Covers <see cref="DatabaseExtensions.CommitSchema(IDatabase, DaoSchemaDefinitionCreateResult)"/>
    /// (bam.data.repositories#1): the script runs as a schema script, which tolerates tables that already exist, rather
    /// than through <c>ExecuteSql</c>, which throws on them once bam.data#28 is in.
    /// </summary>
    [UnitTestMenu("CommitSchema Should", Selector = "css")]
    public class CommitSchemaShould : UnitTestMenuContainer
    {
        public CommitSchemaShould(ServiceRegistry serviceRegistry) : base(serviceRegistry)
        {
        }

        [UnitTest]
        public void RunTheScriptAsASchemaScript()
        {
            When.A<ScriptRecordingDatabase>("commits a schema definition",
                NewDatabase,
                database =>
                {
                    try
                    {
                        TypeSchemaScriptWriter writer = new TypeSchemaScriptWriter();
                        writer.WriteSchemaScript(database, typeof(RepositoryCommitEntity));
                        ((IDatabase)database).CommitSchema(writer.LastSchemaDefinitionCreateResult);
                        return new CommitOutcome(database.SchemaScripts, database.Statements);
                    }
                    finally
                    {
                        Cleanup(database);
                    }
                })
                .TheTest
                .ShouldPass<CommitOutcome>((because, outcome) =>
                {
                    because.ItsTrue($"the commit runs as a schema script ({outcome.SchemaScripts})", outcome.SchemaScripts == 1);
                    because.ItsTrue($"never as a plain ExecuteSql ({outcome.Statements})", outcome.Statements == 0);
                })
                .SoBeHappy()
                .UnlessItFailed();
        }

        private static ScriptRecordingDatabase NewDatabase()
        {
            string directory = Path.Combine(Path.GetTempPath(), "bam_data_repositories_tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(directory);
            return new ScriptRecordingDatabase(directory);
        }

        private static void Cleanup(SQLiteDatabase database)
        {
            try
            {
                string directory = Path.GetDirectoryName(database.DatabaseFile?.FullName) ?? string.Empty;
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
            catch (IOException)
            {
                // best effort: SQLite may still hold the file briefly
            }
        }

        /// <summary>
        /// A SQLite database that counts, without running, the schema scripts and plain builder statements it is asked to
        /// execute. It re-implements <see cref="IDatabase"/> so the count works whether or not the bam.data it builds
        /// against defines <c>ExecuteSchemaScript</c> itself.
        /// </summary>
        private sealed class ScriptRecordingDatabase : SQLiteDatabase, IDatabase
        {
            public ScriptRecordingDatabase(string directory) : base(directory, "BamDataRepositoriesCommit")
            {
            }

            public int SchemaScripts { get; private set; }

            public int Statements { get; private set; }

            void IDatabase.ExecuteSchemaScript(ISqlStringBuilder builder)
            {
                ((IDatabase)this).ExecuteSchemaScript(builder, ServiceProvider.Get<IParameterBuilder>());
            }

            void IDatabase.ExecuteSchemaScript(ISqlStringBuilder builder, IParameterBuilder parameterBuilder)
            {
                SchemaScripts++;
            }

            public override void ExecuteSql(ISqlStringBuilder builder, IParameterBuilder parameterBuilder)
            {
                Statements++;
            }
        }

        private sealed record CommitOutcome(int SchemaScripts, int Statements);
    }

    /// <summary>A plain type for the schema writer to create a table from.</summary>
    public class RepositoryCommitEntity
    {
        public ulong Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
