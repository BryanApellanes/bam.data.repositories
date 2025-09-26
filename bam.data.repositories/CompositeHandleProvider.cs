using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Data.Repositories
{
    // TODO: uise this to generate hash keys for entities based on properties with [KeyHandle]
    // see DeviceData, NicData, MachineData for examples
    public class CompositeHandleProvider : ICompositeHandleProvider
    {
        public HashAlgorithms HashAlgorithm { get; set; } = HashAlgorithms.SHA256;
        public string GetKeyHandle(object instance)
        {
            Args.ThrowIfNull(instance, nameof(instance));
            Type type = instance.GetType();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (PropertyInfo property in type.GetPropertiesWithAttributeOfType<CompositeHandleAttribute>())
            {
                object? value = property.GetValue(instance);
                if (value != null)
                {
                    if(value is Array arr)
                    {
                        object[] objects = arr.Cast<object>().ToArray();
                        Array.ForEach<object>(objects, item => stringBuilder.Append(item.ToString()));
                    }
                    else
                    {
                        stringBuilder.Append(value.ToString());
                    }
                    stringBuilder.Append("\r\n");
                }
            }

            return stringBuilder.ToString().HashHexString(HashAlgorithm);
        }
    }
}
