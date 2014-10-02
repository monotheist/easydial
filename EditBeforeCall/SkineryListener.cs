using System;
using System.Windows.Navigation;

namespace Sym
{
    class SkineryListener : UriMapperBase
    {
        public override Uri MapUri(Uri uri)
        {
            String tempUri = System.Net.HttpUtility.UrlDecode(uri.ToString());

            if (tempUri.Contains(":referrer="))
            {
                // Get the referrer
                int referrerIdIndex = tempUri.IndexOf("referrer=") + 9;
                string referrer = tempUri.Substring(referrerIdIndex);
                System.Diagnostics.Debug.WriteLine("referrer = " + referrer);

                return new Uri("/MainPage.xaml", UriKind.Relative);
            }

            // Otherwise perform normal launch.
            return uri;
        }
    }
}