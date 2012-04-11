using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using Newtonsoft.Json;

namespace ZyncroApiSample
{
    class ZyncroApiSample
    {
        private const String ApiKey = "ApiKey";
        private const String ApiSecret = "ApiSecret";

        private const String ZyncroURL = "https://my.sandbox.zyncro.com/";
        private const String RequestTokenURL = ZyncroURL + "tokenservice/oauth/v1/get_request_token";
        private const String NoBrowserAuthorizationURL = ZyncroURL + "tokenservice/oauth/v1/NoBrowserAuthorization";
        private const String AccessTokenURL = ZyncroURL + "tokenservice/oauth/v1/get_access_token";

        static void Main(string[] args)
        {
            String email = "Email";
            String password = "Password";
            
            // Get an Access token for a user
            IToken accessToken = GetAccessTokenForUser(email, password);

            // Get the main Microblogging for a user
            XmlNode response = GetMainFeed(accessToken);
            Console.WriteLine("Main feed: " + response.InnerXml);

            // Publish a new message in User's Personal feed
            String eventId = PublishOnPersonalFeed(accessToken, "Hello world, Zyncro!");
            Console.WriteLine("New Event published: " + eventId);

            Console.WriteLine("Press any key to finish...");
            Console.ReadKey();
        }

        private static XmlNode GetMainFeed(IToken accessToken)
        {
            XmlNode node = null;
            var url = ZyncroURL + "api/v1/rest/wall";
            var parameters = new Dictionary<string, string>();
            var headers = new Dictionary<string, string>();
            try
            {
                String response = SendGetZyncro(url, accessToken, parameters, headers);
                node = JsonConvert.DeserializeXmlNode(response, "json");                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetMainFeed: " + ex.Message);
            }
            return node;
        }

        private static String PublishOnPersonalFeed(IToken accessToken, String comment)
        {
            String response = null;

            var url = ZyncroURL + "api/v1/rest/wall/personalfeed";
            comment = HttpUtility.UrlEncode(comment, Encoding.UTF8);

            var parameters = new Dictionary<string, string> { { "comment", comment } };
            var headers = new Dictionary<string, string>() { { "X-Zyncro-Params-Encoded-API", "true" } };
            try
            {
                response = SendPostZyncroApi(url, accessToken, parameters, headers);                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in PublishOnPersonalFeed: " + ex.Message);
            }
            return response;
        }        

        private static String SendGetZyncro(string serviceUrl, IToken accessToken, Dictionary<String, String> parameters, Dictionary<String, String> headers)
        {
            String response = null;
            try
            {
                IOAuthSession session = CreateOAuthSession();
                session.AccessToken = accessToken;

                response = session.Request()
                    .Get()
                    .ForUrl(serviceUrl)
                    .WithQueryParameters(parameters)
                    .ReadBody();
            }
            catch (WebException webEx)
            {
                throw new Exception(webEx.Message);
            }
            return response;
        }

        private static String SendPostZyncroApi(string serviceUrl, IToken accessToken, Dictionary<String, String> parameters, Dictionary<String, String> headers)
        {
            String response = null;            
            try
            {
                IOAuthSession session = CreateOAuthSession();
                session.AccessToken = accessToken;
                
                response = session.Request()
                    .Post()
                    .WithHeaders(headers)
                    .ForUrl(serviceUrl)
                    .WithFormParameters(parameters)
                    .ReadBody();
            }
            catch (WebException webEx)
            {
                throw new Exception(webEx.Message);
            }
            return response;
        }

        private static IToken GetAccessTokenForUser(String email, String password)
        {
            IOAuthSession session = CreateOAuthSession();
            var requestToken = session.GetRequestToken("POST");
            if (string.IsNullOrEmpty(requestToken.Token))
            {
                throw new Exception("The request token was null or empty");
            }
            var userId = AuthorizeToken(email, password, requestToken.Token);
            if (string.IsNullOrEmpty(userId))
            {
                throw new Exception("Invalid email or password");
            }

            return session.ExchangeRequestTokenForAccessToken(requestToken, "POST", null);
        }

        private static string AuthorizeToken(string username, string password, string token)
        {            
            var request = (HttpWebRequest) WebRequest.Create(NoBrowserAuthorizationURL);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            var requestStream = request.GetRequestStream();
            var data = new ASCIIEncoding().GetBytes(String.Format("request_token={0}&URL=&password={1}&username={2}", HttpUtility.UrlEncode(token), HttpUtility.UrlEncode(password), HttpUtility.UrlEncode(username)));
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();

            string result = null;
            System.Net.WebResponse response = request.GetResponse();            
            if (((System.Net.HttpWebResponse)(response)).StatusCode == HttpStatusCode.OK)
            {
                result = response.Headers["oauth_userid"];
            }
            return result;
        }

        private static IOAuthSession CreateOAuthSession()
        {
            var consumerContext = new OAuthConsumerContext
            {
                ConsumerKey = ApiKey,
                ConsumerSecret = ApiSecret,
                Realm = "",
                SignatureMethod = "HMAC-SHA1",
                UseHeaderForOAuthParameters = true
            };
            return new OAuthSession(consumerContext, RequestTokenURL, NoBrowserAuthorizationURL, AccessTokenURL);
        }

    }
}
