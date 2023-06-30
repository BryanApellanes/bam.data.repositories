using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Data.Repositories
{
    public interface IWrapperModelRenderProvider
    {
        string Render(string templateName, WrapperModel wrapperModel);
    }
}
