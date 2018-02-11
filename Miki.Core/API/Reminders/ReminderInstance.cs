using IA.SDK.Builders;
using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Miki.Core.API.Reminder
{
    public class ReminderInstance
    {
		ReminderContainer parent;
		CancellationTokenSource cancellationToken;

		public ulong UserId => parent.Id;
		public int ReminderId;
		public string Text;

		public DateTime StartTime = DateTime.Now;
		public TimeSpan Length;

		public TimeSpan TimeLeft => FinishedAt - DateTime.Now;
		public DateTime FinishedAt => StartTime + Length;

		public bool RepeatReminder { get; set; }

		public ReminderInstance(int id, ReminderContainer parent, string text)
		{
			ReminderId = id;
			this.parent = parent;
			Text = text;
			cancellationToken = new CancellationTokenSource();
		}

		public void Start(IDiscordUser user)
		{
			Task.Run(() => RunTask(user), cancellationToken.Token);
		}
		public void Start(IDiscordMessageChannel channel)
		{
			Task.Run(() => RunTask(channel), cancellationToken.Token);
		}

		public async Task RunTask(IDiscordUser user)
		{
			await Task.Delay((int)Length.TotalMilliseconds);

			cancellationToken.Token.ThrowIfCancellationRequested();

			await CreateReminderEmbed(Text)
				.QueueToUser(user);

			if (RepeatReminder)
			{
				parent.CreateNewReminder(user, Text, Length, RepeatReminder);
			}
			parent.RemoveReminder(ReminderId);
		}
		public async Task RunTask(IDiscordMessageChannel channel)
		{
			await Task.Delay((int)Length.TotalMilliseconds);

			cancellationToken.Token.ThrowIfCancellationRequested();

			await CreateReminderEmbed(Text)
				.QueueToChannel(channel);

			if (RepeatReminder)
			{
				parent.CreateNewReminder(channel, Text, Length, RepeatReminder);
			}
			parent.RemoveReminder(ReminderId);
		}

		public IDiscordEmbed CreateReminderEmbed(string text)
		{
			return Utils.Embed
			.SetTitle("⏰ Reminder")
			.SetDescription(new MessageBuilder()
			   .AppendText(Text)
			   .BuildWithBlockCode());
		}

		public void Cancel()
		{
			cancellationToken.Cancel();
			parent.RemoveReminder(ReminderId);
		}
	}
}
