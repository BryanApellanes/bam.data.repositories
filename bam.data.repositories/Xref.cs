/*
	Copyright © Bryan Apellanes 2015  
*/

namespace Bam.Data.Repositories
{
	public class Xref
	{
		public Xref() { }
		public Xref(object left, object right)
		{
			this.Left = left;
			this.Right = right;
		}

		public object Left { get; private set; } = null!;
		public object Right { get; private set; } = null!;
	}
}
