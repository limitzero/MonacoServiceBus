using System;

namespace Monaco
{
	[Obsolete]
	public interface IEndpoint
	{
		bool IsTransactional { get; set; }
		Uri Uri { get; set; }
	}
}