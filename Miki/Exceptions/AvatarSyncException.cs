using Miki.Framework.Exceptions;
using Miki.Localization;
using Miki.Localization.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Exceptions
{
	public class AvatarSyncException : LocalizedException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_avatar_sync");

		public AvatarSyncException() : base()
		{ }
	}
}
