using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.API.Reminders
{
    public interface IJob
    {
		int JobId { get; }

		string Name { get; }

		bool Repeating { get; }

		ulong SessionId { get; }

		DateTime StartedAt { get; }

		TimeSpan TimeLeft { get; }
		DateTime TicksAt { get; }

		void Cancel();

		void Start();
	}
}
