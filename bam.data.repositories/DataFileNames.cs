using Bam.Data.Schema;
using Bam.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Data.Repositories
{
    public class DataFileNames
    {
        public DataFileNames(DataNamespaces dataNamespaces)
        {
            this.DataNamespaces = dataNamespaces;
        }

        public DataNamespaces DataNamespaces { get; private set; }

        public string GetWrapperAssemblyName()
        {
            return $"{this.DataNamespaces.WrapperNamespace}.dll";
        }

        public string GetWrapperCodeFileName(Type type, string fileExtension = ".cs")
        {
            return $"{type.Name.TrimNonLetters()}Wrapper{fileExtension}";
        }
    }
}
