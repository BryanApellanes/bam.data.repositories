using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Data.Repositories
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CompositeHandleAttribute : Attribute
    {
    }
}
