namespace Miki.Services
{
    using System;
    using Miki.Localization.Exceptions;
    using Miki.Localization;

    public class FailedTransactionException : LocalizedException
    {
        /// <inheritdoc />
        public override IResource LocaleResource 
            => new LanguageResource("miki_error_transaction_failed");

        public FailedTransactionException(Exception innerException)
            : base("Transaction was not able to complete.", innerException)
        { }
    }
}
