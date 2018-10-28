using System;

namespace Miki.Exceptions
{
	/// <summary>
	/// Used to throw errors, but not requeue the message
	/// </summary>
	public class RabbitException : Exception
	{
	}
}