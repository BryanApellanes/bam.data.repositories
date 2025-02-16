using System.Reflection;

namespace Bam.CoreServices.ProtoBuf
{
    public interface IPropertyNumberer
    {
        int GetNumber(Type type, PropertyInfo prop);
    }
}