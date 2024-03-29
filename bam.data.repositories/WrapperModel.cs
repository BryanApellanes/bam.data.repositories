/*
	Copyright © Bryan Apellanes 2015  
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bam.Net.Data.Schema;
using Bam.Net.ServiceProxy;
using System.Reflection;
using Bam.Net.Data.Repositories;
using Bam.Net;

namespace Bam.Data.Repositories
{
    public class WrapperModel : IRenderable
	{
		public WrapperModel(Type pocoType, ITypeSchema schema, string wrapperNamespace = "TypeWrappers", string daoNameSpace = "Daos")
		{
			BaseType = pocoType;
			WrapperNamespace = wrapperNamespace;
            DaoNamespace = daoNameSpace;
			TypeNamespace = pocoType.Namespace ?? "Types";
            TypeName = pocoType.Name.TrimNonLetters();
            WrapperTypeName = pocoType.ToTypeString(false).Replace(TypeName, $"{TypeName}Wrapper");
            BaseTypeName = pocoType.ToTypeString(false);
			ForeignKeys = schema.ForeignKeys.Where(fk => fk.PrimaryKeyType.Equals(pocoType)).ToArray();
			ChildPrimaryKeys = schema.ForeignKeys.Where(fk => fk.ForeignKeyType.Equals(pocoType)).ToArray();
            LeftXrefs = schema.Xrefs.Where(xref => xref.Left.Equals(pocoType)).Select(xref => TypeXrefModel.FromTypeXref(xref, daoNameSpace)).ToArray();
            RightXrefs = schema.Xrefs.Where(xref => xref.Right.Equals(pocoType)).Select(xref => TypeXrefModel.FromTypeXref(xref, daoNameSpace)).ToArray();
		}

		public ITemplateRenderer TemplateRenderer { get; set; }

        public virtual string Render()
        {
            return TemplateRenderer.Render("Wrapper", this);//return Bam.Net.Handlebars.Render("Wrapper", this);
        }

        public void Render(Stream output)
        {
            throw new NotImplementedException();
        }

        public void Render(ITemplateRenderer renderer)
        {
            throw new NotImplementedException();
        }

        public void Render(ITemplateRenderer renderer, string templateName, Stream output)
        {
            throw new NotImplementedException();
        }

        public string WrapperNamespace { get; set; }

		public string TypeNamespace { get; set; }
        public string DaoNamespace { get; set; }
        public string TypeName { get; set; }
		public string WrapperTypeName { get; set; }
        public string BaseTypeName { get; set; }

		public ITypeFk[] ForeignKeys { get; set; }

		public ITypeFk[] ChildPrimaryKeys { get; set; }

		/// <summary>
		/// Xrefs where the current DtoType is the left
		/// side of the cross reference table
		/// </summary>
		public TypeXrefModel[] LeftXrefs { get; set; }

		/// <summary>
		/// Xrefs where the current DtoType is the Right
		/// side of the cross reference table
		/// </summary>
		public TypeXrefModel[] RightXrefs { get; set; }
		
		public Type BaseType { get; set; }
	}
}
