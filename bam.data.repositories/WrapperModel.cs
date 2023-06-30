using Bam.Net.CommandLine;
using Bam.Net.Data.Schema;
using Bam.Net.ServiceProxy;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Bam.Data.Repositories
{
    public partial class WrapperModel
    {
        public IWrapperModelRenderProvider RenderProvider { get; set; }
        public virtual string Render()
        {
            return RenderProvider.Render("Wrapper", this);//return Bam.Net.Handlebars.Render("Wrapper", this);
        }
    }
}
