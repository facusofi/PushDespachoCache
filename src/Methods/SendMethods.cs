using System;
using JsonRequest;
using System.Configuration;
using ShamanClases_CSharp;
using ModelTelmed;

namespace Methods
{
    public class SendMethods
    {

        Request request = new Request();
        private string urlToken;
        private string urlMethod;

        public SendMethods(string _urlMethod)
        {
            urlToken = "";
            urlMethod = _urlMethod;
        }

        public SendMethods(string _urlToken, string _urlMethod)
        {
            urlToken = _urlToken;
            urlMethod = _urlMethod;
        }

        /// <summary>
        ///  Telmed
        /// </summary>
        public ConferenceBO NewWithUser(EmergencyDTO.TeleasistenciaReq req, string jwt) => (ConferenceBO)request.Execute<ConferenceBO>(urlMethod, req, jwt, "POST");
        public TokenInfoRsp GetToken() => (TokenInfoRsp)request.Execute<TokenInfoRsp>(urlToken, null, null, "GET");
        public string NewLink(NewLinkReq req, string jwt) => (string)request.Execute<string>(urlMethod, req, jwt, "POST");

        /// <summary>
        ///  CyT
        /// </summary>
        public bool Outgoing_Message() => request.Execute(urlMethod, null, "", "GET");


    }
}
