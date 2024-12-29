using LuminaPath.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.Core.Common.Interfaces
{
	public interface IMedia<TDocument>: IBasicInfo where TDocument : IDocument
	{
		public TDocument? Image { get; set; }
	}
}
