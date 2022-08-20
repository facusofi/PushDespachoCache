using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using NLog;

namespace JsonRequest
{
    /// <summary>
    /// Create Http Request, using json, and read Http Response.
    /// </summary>
    public class Request
    {

        private static Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Url of http server wich request will be created to.
        /// </summary>
        public string URL { get; set; }

        /// <summary>
        /// HTTP Verb wich will be used. Eg. GET, POST, PUT, DELETE.
        /// </summary>
        public string Verb { get; set; }

        /// <summary>
        /// Modo de autorización del request
        /// </summary>
        public string Authorization { get; set; }

        /// <summary>
        /// Request content, Json by default.
        /// </summary>
        public string Content
        {
            get { return "application/json"; }
        }

        /// <summary>
        /// User and Password for Basic Authentication
        /// </summary>
        public Credentials Credentials { get; set; }

        public HttpWebRequest HttpRequest { get; internal set; }
        public HttpWebResponse HttpResponse { get; internal set; }

        public CookieContainer CookieContainer = new CookieContainer();

        /// <summary>
        /// Constructor Overload that allows passing URL and the VERB to be used.
        /// </summary>
        /// <param name="url">URL which request will be created</param>
        /// <param name="verb">Http Verb that will be userd in this request</param>
        /// <param name="Authorization">Método de Autorizacion</param>
        public Request(string url, string verb, string authorization = "Bearer")
        {
            URL = url;
            Verb = verb;
            Authorization = authorization;
        }

        /// <summary>
        /// Default constructor overload without any paramter
        /// </summary>
        public Request()
        {
            Verb = "GET";
        }

        public object Execute<TT>(string url, object obj, string jwt, string verb, string authorization = "Bearer")
        {
            if (url != null)
                URL = url;

            if (verb != null)
                Verb = verb;

            Authorization = authorization;

            HttpRequest = CreateRequest(jwt);

            WriteStream(obj);

            try
            {
                HttpResponse = (HttpWebResponse)HttpRequest.GetResponse();
            }
            catch (WebException error)
            {
                HttpResponse = (HttpWebResponse)error.Response;
                logger.Debug(string.Format("Execute - Con error en GetResponse: {0}", ReadResponseFromError(error)));
                return null;
            }
            try
            {
                var response = ReadResponse();
                return JsonConvert.DeserializeObject<TT>(response);
            }
            catch (JsonSerializationException error)
            {
                //LOG Error de conversion.
                logger.Debug(string.Format("Execute - JsonSerializationException: {0}", error.Message));
                return null;
            }
        }

        internal HttpWebRequest CreateRequest(string jwt)
        {
            var basicRequest = (HttpWebRequest)WebRequest.Create(URL);
            basicRequest.ContentType = Content;
            basicRequest.Method = Verb;
            basicRequest.CookieContainer = CookieContainer;

            if (Credentials != null)
                basicRequest.Headers.Add("Authorization", "Basic" + " " + EncodeCredentials(Credentials));
            
            if (!string.IsNullOrEmpty(jwt))
                basicRequest.Headers.Add("Authorization", Authorization + " " + jwt);

            return basicRequest;
        }

        internal void WriteStream(object obj)
        {
            if (obj != null)
            {

                //using (var streamWriter = new StreamWriter("file.txt"))
                //{
                //    if (obj is string)
                //        streamWriter.Write(obj);
                //    else
                //        streamWriter.Write(JsonConvert.SerializeObject(obj));
                //}

                using (var streamWriter = new StreamWriter(HttpRequest.GetRequestStream()))
                {
                    if (obj is string)
                        streamWriter.Write(obj);
                    else
                        streamWriter.Write(JsonConvert.SerializeObject(obj));
                }
            }
        }

        internal string ReadResponse()
        {
            if (HttpResponse != null)
                using (var streamReader = new StreamReader(HttpResponse.GetResponseStream()))
                    return streamReader.ReadToEnd();

            return string.Empty;
        }

        internal string ReadResponseFromError(WebException error)
        {
            using (var streamReader = new StreamReader(error.Response.GetResponseStream()))
                return streamReader.ReadToEnd();
        }

        internal static string EncodeCredentials(Credentials credentials)
        {
            var strCredentials = string.Format("{0}:{1}", credentials.UserName, credentials.Password);
            var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(strCredentials));

            return encodedCredentials;
        }
    }
}