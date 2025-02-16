using Bam.Analytics;

namespace Bam.Data.Repositories
{
    public class SchemaDifferenceEventArgs: EventArgs
    {
        public TypeSchema TypeSchema { get; set; }
        public GeneratedDaoAssemblyInfo GeneratedDaoAssemblyInfo { get; set; }
        public IDiffReport DiffReport { get; set; }
    }
}
