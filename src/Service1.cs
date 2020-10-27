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

namespace FichadaRelojUyService
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
            t.Elapsed += delegate { ElapsedHandler(); };
            t.Interval = Convert.ToInt32(timePool) * 1000;
            t.Start();
            Logger.GetInstance().AddLog(true, "OnStart", "Servicio inicializado.");
            CustomMail mail = new CustomMail(MailType.Information, "Se ha inicializado el servicio de PushDespachoCache", "Servicio de Despacho Push");

            mail.Send();
        }

        protected override void OnPause()
        {
            Logger.GetInstance().AddLog(true, "OnPause", "Se ejecutó el método OnPause, el servicio deja de estar activo.");
            CustomMail mail = new CustomMail(MailType.Information, "Se ha pausado el servicio de PushDespachoCache", "Servicio de Despacho Push");
            mail.Send();
            t.Stop();
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

            /// Connect Cache
            ConnectionStringCache cnnCache = new ConnectionStringCache();

            cnnCache.Server = ConfigurationManager.AppSettings["Server"];
            cnnCache.Port = ConfigurationManager.AppSettings["Port"];
            cnnCache.Namespace = ConfigurationManager.AppSettings["Namespace"];
            cnnCache.UserID = ConfigurationManager.AppSettings["UserID"];
            cnnCache.Password = ConfigurationManager.AppSettings["Password"];

            PanelC.MensajesPager objMensajeria = new PanelC.MensajesPager(cnnCache);

            DataTable dt = objMensajeria.GetPushAppPendientes();

            for (int i = 0; i < dt.Rows.Count - 1; i++)
            {

                string oneSignalUrl;
                string result;
                bool sended = false; 

                oneSignalUrl = string.Format("{0}?license={1}&mobileNumber={2}&message={3}&header=Shaman SGE", ConfigurationManager.AppSettings["oneSignalUrl"], ConfigurationManager.AppSettings["license"], dt.Rows[i]["MovilId"].ToString(), dt.Rows[i]["Mensaje"].ToString());

                using (WebClient client = new WebClient())
                {
                    result = client.DownloadString(oneSignalUrl);
                }

                if ((!string.IsNullOrEmpty(result)) && (result.ToLower() == "true"))
                {
                    sended = true;
                }

                /// HotFix DB Troubles Omni Cache
                sended = true;

                objMensajeria.SetEstadoMensaje(Convert.ToDecimal(dt.Rows[i]["ID"]), sended);

            }

            objMensajeria = null;

        }

    }
}
