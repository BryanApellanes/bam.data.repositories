using Bam.Data.Schema;
using Bam.Data;
using Bam.Data.Repositories;

namespace Bam.CoreServices.ProtoBuf
{
    /// <summary>
    /// A ProtoFileGenerator that will only
    /// include properties addorned with the 
    /// custom attribute ColumnAttribute
    /// </summary>
    public class DaoProtoFileGenerator : ProtoFileGenerator
    {
        public DaoProtoFileGenerator(IPropertyNumberer propertyNumberer) 
            : this(new SchemaProvider(), propertyNumberer)
        { }

        public DaoProtoFileGenerator(ISchemaProvider typeSchemaGenerator, IPropertyNumberer propertyNumberer) 
            : base(typeSchemaGenerator, propertyNumberer, (pi)=>pi.HasCustomAttributeOfType<ColumnAttribute>())
        { }
    }
}
