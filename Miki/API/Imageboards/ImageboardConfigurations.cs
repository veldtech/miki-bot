using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API.Imageboards
{
    public class ImageboardConfigurations
    {
        public string QueryKey = "";

        public string ExplicitTag = "rating:explicit";
        public string QuestionableTag = "rating:questionable";
        public string SafeTag = "rating:safe";

        public bool NetUseCredentials = false;
        public ICredentials NetCredentials = CredentialCache.DefaultCredentials;
        public List<string> NetHeaders = new List<string>();
    }
}
