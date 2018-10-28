using Miki.Localization;
using Miki.Models;

namespace Miki.Exceptions
{
	public class DuplicatePastaException : PastaException
	{
		public override IResource LocaleResource
			=> new LanguageResource("miki_module_pasta_create_error_already_exist", $"`{_pasta.Id}`");

		public DuplicatePastaException(GlobalPasta pasta) : base(pasta)
		{
		}
	}
}