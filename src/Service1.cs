using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using System.Net;
using System.Reflection;
using System.Configuration;
using ShamanClases_CSharp;
using System.Net.Mail;
using System.Data.SqlClient;
using Newtonsoft.Json;

namespace PushDespachoCache
{
    public partial class Service1 : ServiceBase
    {

        Timer t = new Timer();

        string timePool = ConfigurationManager.AppSettings["TimePool"];

        public Service1()
        {
            InitializeComponent();
        }
        protected override void OnStart(string[] args)
        {
            new System.Threading.Thread(StartService).Start();
        }


        protected override void OnPause()
        {
            Logger.GetInstance().AddLog(true, "OnPause", "Se ejecutó el método OnPause, el servicio deja de estar activo.");
            CustomMail mail = new CustomMail(MailType.Information, "Se ha pausado el servicio de PushDespachoCache", "Servicio de Despacho Push");
            mail.Send();
            t.Stop();
        }

        internal void StartService()
        {
            t.Elapsed += delegate { ElapsedHandler(); };
            t.Interval = Convert.ToInt32(timePool) * 1000;
            t.Start();
            Logger.GetInstance().AddLog(true, "OnStart", "Servicio inicializado.");
            CustomMail mail = new CustomMail(MailType.Information, "Se ha inicializado el servicio de PushDespachoCache", "Servicio de Despacho Push");

            mail.Send();
        }

        protected override void OnContinue()
        {
            Logger.GetInstance().AddLog(true, "OnPause", "Se ejecutó el método OnContinue, el servicio vuelve a estar activo.");
            t.Start();
        }

        protected override void OnStop()
        {
            Logger.GetInstance().AddLog(true, "OnStop", "Se ejecutó el método OnStop, el servicio deja de estar activo.");
            CustomMail mail = new CustomMail(MailType.Information, "Se ha detenido el servicio de PushDespachoCache", "Servicio de Despacho Push");
            mail.Send();
            t.Stop();
        }

        public void ElapsedHandler()
        {
            if (Convert.ToInt16(ConfigurationManager.AppSettings["runPushDespacho"]) == 1)
            {
                //Logger.GetInstance().AddLog(true, "CallPushAndroid", "Énvía las push notifications de Despacho");
                CallPushAndroid();
            }

            if (Convert.ToInt16(ConfigurationManager.AppSettings["runTeleasistencia"]) == 1)
            {
                //Logger.GetInstance().AddLog(true, "CallTeleasistencia", "Envio los pendientes de teleasistencia");
                CallTeleasistencia();
            }

        }

        public void CallPushAndroid()
        {

            /// Connect Cache
            ConnectionStringCache cnnCache = this.getConnectionString();

            Logger.GetInstance().AddLog(true, "CallPushAndroid", "Buscando Mensajes para Push");

            PanelC.MensajesPager objMensajeria = new PanelC.MensajesPager(cnnCache);
            DataTable dt = objMensajeria.GetPushAppPendientes();

            Logger.GetInstance().AddLog(true, "CallPushAndroid", string.Format("Se encontraron {0} mensajes", dt.Rows.Count.ToString()));

            for (int i = 0; i <= dt.Rows.Count - 1; i++)
            {

                try
                {
                    string oneSignalUrl;
                    string result;
                    bool sended = false;

                    oneSignalUrl = string.Format("{0}?license={1}&mobileNumber={2}&message={3}&header=Shaman SGE", ConfigurationManager.AppSettings["oneSignalUrl"], ConfigurationManager.AppSettings["license"], dt.Rows[i]["MovilId"].ToString(), dt.Rows[i]["Mensaje"].ToString());

                    Logger.GetInstance().AddLog(true, "CallPushAndroid", string.Format("Enviando {0}", oneSignalUrl));

                    using (WebClient client = new WebClient())
                    {
                        result = client.DownloadString(oneSignalUrl);
                    }

                    if ((!string.IsNullOrEmpty(result)) && (result.ToLower() == "true"))
                    {
                        sended = true;
                    }

                    Logger.GetInstance().AddLog(true, "CallPushAndroid", string.Format("Enviado mensaje {0} = {1}", dt.Rows[i]["ID"].ToString(), sended.ToString()));

                    objMensajeria.SetEstadoMensaje(Convert.ToDecimal(dt.Rows[i]["ID"]), sended);

                }
                
                catch (Exception ex)
                {
                    Logger.GetInstance().AddLog(true, "CallPushAndroid", ex.Message);
                }

            }

            objMensajeria = null;

        }

        public void CallTeleasistencia()
        {
            /// Connect Cache
            ConnectionStringCache cnnCache = this.getConnectionString();

            EmergencyC.IncPendientesTeleasistencia incPendientesTeleasistencia = new EmergencyC.IncPendientesTeleasistencia(cnnCache);
            List<EmergencyDTO.TeleasistenciaReq> listTeleasistenciaReq = incPendientesTeleasistencia.GetPendientes();

            if (listTeleasistenciaReq != null)
            {
                foreach (EmergencyDTO.TeleasistenciaReq teleasistenciaReq in listTeleasistenciaReq)
                {
                    string url = ConfigurationManager.AppSettings["AIDShamanAPI_URL"];

                    //Creo la llamada al WS
                    WebRequest request = WebRequest.Create(url); //"https://msfy-backend.herokuapp.com/auth/signin"
                    request.Credentials = CredentialCache.DefaultCredentials;
                    request.Method = "POST";
                    request.ContentType = "application/json";

                    //Preparo el objeto a enviar
                    string stringData = JsonConvert.SerializeObject(teleasistenciaReq);
                    byte[] sBytes = Encoding.UTF8.GetBytes(stringData);
                    request.ContentLength = sBytes.Length;
                    Stream dataStream = request.GetRequestStream();
                    dataStream.Write(sBytes, 0, sBytes.Length);
                    dataStream.Close();
                    WebResponse response = request.GetResponse();

                    //RETURN
                    string req = string.Empty;
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                        req = reader.ReadToEnd();
                        dynamic oReturn = JsonConvert.DeserializeObject(req);
                        incPendientesTeleasistencia.SaveConferenceID(teleasistenciaReq.ShamanIncidenteID, Convert.ToInt64(oReturn.ConferenceId));
                        //Grabar la ConferenciaID

                    }
                    try
                    {
                        dynamic oResponse = JsonConvert.DeserializeObject<dynamic>(req);

                    }
                    catch (JsonReaderException js)
                    {
                        throw new Exception("JS: " + js.Message);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }

        public ConnectionStringCache getConnectionString()
        {
            try
            {
                ConnectionStringCache cnnCache = new ConnectionStringCache();
                cnnCache.Server = ConfigurationManager.AppSettings["Server"];
                cnnCache.Port = ConfigurationManager.AppSettings["Port"];
                cnnCache.Namespace = ConfigurationManager.AppSettings["Namespace"];
                cnnCache.Password = Encrypt.EncryptString(ConfigurationManager.AppSettings["Password"], "javiernigrelli");
                cnnCache.UserID = Encrypt.EncryptString(ConfigurationManager.AppSettings["UserID"], "javiernigrelli");

                return cnnCache;
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
