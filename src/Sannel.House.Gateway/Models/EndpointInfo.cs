using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sannel.House.Gateway.Models
{
	public class EndpointInfo
	{
		public string Name { get; set; }
		public Uri Path { get; set; }

		public Rewrite[] Rewrite { get; set; }
	}
}
