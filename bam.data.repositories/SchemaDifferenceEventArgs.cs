using Bam.Analytics;

namespace Bam.Data.Repositories
{
    public class SchemaDifferenceEventArgs: EventArgs
    {
        public TypeSchema TypeSchema { get; set; } = null!;
        public GeneratedDaoAssemblyInfo GeneratedDaoAssemblyInfo { get; set; } = null!;
        public IDiffReport DiffReport { get; set; } = null!;
    }
}
