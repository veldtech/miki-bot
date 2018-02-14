using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Framework.Interactive
{
    internal interface IProcessable<T>
    {
		T Process();
    }

	internal interface ISceneResponse
	{
		bool IsSuccess { get; }
	}
}
