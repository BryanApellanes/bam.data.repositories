using System.Reflection;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Bam.Data.Repositories;

public class DataHandle : SortedDictionary<string, string>
{
    public DataHandle()
    {
        this.HashAlgorithm = HashAlgorithms.SHA256;
        this.Stringifier = (o) => o.ToJson();
    }

    public DataHandle(CompositeKeyAuditRepoData data) : this()
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }
        Type type = data.GetType();
        this.Add("type", type.AssemblyQualifiedName ?? type.FullName ?? type.Name ?? throw new InvalidOperationException("unable to determine type name"));
        this.AddCompositeKeys(data);
    }
    
    [YamlIgnore]
    [JsonIgnore]
    public Func<object, string> Stringifier { get; set; }
    
    [YamlIgnore]
    [JsonIgnore]
    public HashAlgorithms HashAlgorithm { get; set; }

    public string Value =>  this.ToJson().HashHexString(HashAlgorithm);

    private void AddCompositeKeys(object data)
    {
        Type type = data.GetType();
        foreach (PropertyInfo property in type.GetProperties()
                     .Where(p => p.CanRead && p.HasCustomAttributeOfType<CompositeKeyAttribute>()))
        {
            object? value = property.GetValue(data);
            Add(property.Name, value == null ? "null": Stringifier(value));
        }
    }
}