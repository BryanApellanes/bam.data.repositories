using Bam.Net.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bam.data.repositories
{
    public interface IWrapperModelRenderProvider
    {
        string Render(string templateName, WrapperModel wrapperModel);
    }
}
