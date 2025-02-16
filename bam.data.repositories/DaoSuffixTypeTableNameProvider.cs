namespace Bam.Data.Repositories
{
    /// <summary>
    /// A table name provider that returns the type name
    /// suffixed with "Dao"
    /// </summary>
    public class DaoSuffixTypeTableNameProvider: ITypeTableNameProvider
    {
        public string GetTableName(Type type)
        {
            return "{0}Dao".Format(type.Name.TrimNonLetters());
        }
    }
}
