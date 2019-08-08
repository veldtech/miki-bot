using Amazon.S3;
using Newtonsoft.Json;
using System.IO;

namespace Miki
{
	/// <summary>
	/// Global data for constant folder structures and versioning.
	/// </summary>
	public class Global
	{
		public static AmazonS3Client CdnClient
		{
			get
			{
				if (cdnClient == null)
				{
					/*cdnClient = new AmazonS3Client(Config.CdnAccessKey, Config.CdnSecretKey, new AmazonS3Config()
					{
						ServiceURL = Config.CdnRegionEndpoint
					});*/
				}
				return cdnClient;
			}
		}

		private static AmazonS3Client cdnClient;
	}

	public class Constants
	{
		public const string NotDefined = "$not-defined";
        public const string ENV_ConStr = "MIKI_CONNSTRING";
        public const string ENV_MsgWkr = "MIKI_MESSAGEWORKER";
        public const string ENV_LogLvl = "MIKI_LOGLEVEL";
        public const string ENV_SelfHost = "MIKI_SELFHOSTED";
        public const string ENV_ConfId = "MIKI_CONFIGID";
	}
}