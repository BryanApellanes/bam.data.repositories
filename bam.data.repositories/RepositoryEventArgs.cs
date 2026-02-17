/*
	Copyright © Bryan Apellanes 2015  
*/

namespace Bam.Data.Repositories
{
	public class RepositoryEventArgs<T> : RepositoryEventArgs
	{
		public RepositoryEventArgs(T data)
			: base(data!)
		{
			this.DataAs = data;
		}

		public RepositoryEventArgs(Exception ex) : base(ex) { }

		public T DataAs { get; set; } = default!;
	}

	public class RepositoryEventArgs: EventArgs
	{
		public RepositoryEventArgs() { }
        public RepositoryEventArgs(object data)
        {
            Data = data;
        }
        public RepositoryEventArgs(object data, Type type) : this(data)
        {
            Type = type;
        }
		public RepositoryEventArgs(Exception ex)
		{
			this.Message = ex.Message;
			if (!string.IsNullOrEmpty(ex.StackTrace))
			{
				this.Message = $"{Message}:\r\nStackTrace: \t{ex.StackTrace}";
			}
		}
        public Type Type { get; set; } = null!;
		public object Data { get; private set; } = null!;

		public string Message { get; set; } = null!;
	}
}
