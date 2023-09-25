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
using Methods;
using ModelTelmed;

using System.Data.OleDb;

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
            this.ElapsedHandler();

            Logger.GetInstance().AddLog(true, "OnStart", "Servicio inicializado.");
            ///CustomMail mail = new CustomMail(MailType.Information, "Se ha inicializado el servicio de PushDespachoCache", "Servicio de Despacho Push");
            //mail.Send();
        }

        protected override void OnContinue()
        {
            Logger.GetInstance().AddLog(true, "OnPause", "Se ejecutó el método OnContinue, el servicio vuelve a estar activo.");
            t.Start();
        }

        protected override void OnStop()
        {
            Logger.GetInstance().AddLog(true, "OnStop", "Se ejecutó el método OnStop, el servicio deja de estar activo.");
            //CustomMail mail = new CustomMail(MailType.Information, "Se ha detenido el servicio de PushDespachoCache", "Servicio de Despacho Push");
            //mail.Send();
            t.Stop();
        }

        public void ElapsedHandler()
        {

            t.Enabled = false;

            if (Convert.ToInt16(ConfigurationManager.AppSettings["runTelmedLinks"]) == 1)
            {
                //Logger.GetInstance().AddLog(true, "CallTelmedLinks", "Envía links telmed por WA");
                this.CallTelmedLinks();
            }

            if (Convert.ToInt16(ConfigurationManager.AppSettings["runSyncSQL"]) == 1)
            {
                //Logger.GetInstance().AddLog(true, "CallSyncSQL", "Sincroniza Cache contra SQL (licencias)");
                this.CallSyncSQL();
            }

            if (Convert.ToInt16(ConfigurationManager.AppSettings["runPushDespacho"]) == 1)
            {
                //Logger.GetInstance().AddLog(true, "CallPushAndroid", "Envía las push notifications de Despacho");
                this.CallPushAndroid();
            }

            if (Convert.ToInt16(ConfigurationManager.AppSettings["runTeleasistencia"]) == 1)
            {
                //Logger.GetInstance().AddLog(true, "CallTeleasistencia", "Envio los pendientes de teleasistencia");
                this.CallTeleasistencia();
            }

            if (Convert.ToInt16(ConfigurationManager.AppSettings["runGPS"]) == 1)
            {
                //Logger.GetInstance().AddLog(true, "CallTeleasistencia", "Registra posicionamiento GPS");
                this.CallGPS();
            }

            if (Convert.ToInt16(ConfigurationManager.AppSettings["runCronos"]) == 1)
            {
                //Logger.GetInstance().AddLog(true, "CallTeleasistencia", "Registra posicionamiento GPS");
                this.CallCronos();
            }

            t.Enabled = true;

        }

        public void CallTelmedLinks()
        {
            /// Connect Cache
            ConnectionStringCache cnnCache = this.getConnectionString();

            Logger.GetInstance().AddLog(true, "CallTelmedLinks", "Buscando Links para WA");

            EmergencyC.TelemedicinaLinks telmedLinks = new EmergencyC.TelemedicinaLinks(cnnCache);

            DataTable dt = telmedLinks.GetPendientes();

            Logger.GetInstance().AddLog(true, "CallTelmedLinks", string.Format("Se encontraron {0} mensajes", dt.Rows.Count.ToString()));

            TokenInfoRsp _tks = null;

            if (dt.Rows.Count > 0)
            {

                /// Armmo Link Telmed...

                string urlLink = string.Format("{0}/Conference/NewLink", ConfigurationManager.AppSettings["AIDShamanAPI_TelmedLink"]);

                string urlToken = string.Format("{0}/Login/GetToken/{1}", ConfigurationManager.AppSettings["AIDShamanAPI_TelmedLink"], ConfigurationManager.AppSettings["license"]);

                string urlReplace = ConfigurationManager.AppSettings["AIDShamanAPI_Replace"];

                SendMethods sm = new SendMethods(urlToken, urlLink);

                if (_tks == null)
                {
                    _tks = sm.GetToken();
                }

                for (int i = 0; i < dt.Rows.Count; i++)
                {

                    DataRow rowInc = dt.Rows[i];

                    try
                    {

                        NewLinkReq newLink = new NewLinkReq();

                        newLink.ShamanIncidenteID = Convert.ToInt64(rowInc["IncidenteId"]);

                        newLink.UserId = Convert.ToInt64(rowInc["AppUserId"]);
                        newLink.FirstName = rowInc["Nombre"].ToString();
                        newLink.LastName = rowInc["Apellido"].ToString();
                        newLink.DNI = Convert.ToInt64(rowInc["NroDocumento"]);
                        newLink.Phone = rowInc["Telefono"].ToString();
                        newLink.Gender = rowInc["Sexo"].ToString();
                        newLink.FecNacimiento = DateToSQL(Convert.ToDateTime(rowInc["FecNacimiento"]));
                        newLink.Email = rowInc["Email"].ToString();
                        newLink.Serial = ConfigurationManager.AppSettings["license"];
                        newLink.Symptom = rowInc["Sintoma"].ToString();

                        newLink.ClientId = Convert.ToInt64(rowInc["ClientId"]);
                        newLink.ClientCode = rowInc["ClienteCode"].ToString();
                        newLink.ClientDescription = rowInc["ClientDescription"].ToString();
                        newLink.ShamanAffiliateNumber = rowInc["ShamanAffiliateNumber"].ToString();

                        newLink.Address_LocationId = Convert.ToInt64(rowInc["LocalidadId"]);
                        newLink.Address_Location = rowInc["Localidad"].ToString();
                        newLink.Address_Street = rowInc["dm_Calle"].ToString();
                        newLink.Address_PostalCode = rowInc["dm_CodigoPostal"].ToString();
                        newLink.Address_Number = rowInc["dm_Altura"].ToString();
                        newLink.Address_Floor = rowInc["dm_Piso"].ToString();
                        newLink.Address_Dpto = rowInc["dm_Departamento"].ToString();
                        newLink.Address_Lat = Convert.ToDecimal(rowInc["dm_Latitud"]);
                        newLink.Address_Lng = Convert.ToDecimal(rowInc["dm_Longitud"]);
                        newLink.Address_BetweenStreet1 = rowInc["dm_EntreCalle1"].ToString();
                        newLink.Address_BetweenStreet2 = rowInc["dm_EntreCalle2"].ToString();
                        newLink.Address_Reference = rowInc["dm_Referencia"].ToString();

                        string linkTelmed = sm.NewLink(newLink, _tks.token);

                        if (!string.IsNullOrEmpty(linkTelmed))
                        {

                            /// Envío WA

                            int metodoId = 1;

                            decimal telefono = telmedLinks.GetTelefono(rowInc["Telefono"].ToString());

                            if (telefono > 9999999)
                            {
                                string urlWA = string.Format("{0}?numDest={1}&nameTemplate={2}&paramsTemplate.1={3}",
                                    rowInc["URL"].ToString(), telefono.ToString(), rowInc["nameTemplate"].ToString(), linkTelmed.Replace(urlReplace, ""));

                                sm = new SendMethods(urlWA);

                                bool result = sm.Outgoing_Message();

                                if (result)
                                {
                                    telmedLinks.SetEnviado(Convert.ToDecimal(rowInc["IncidenteId"]), metodoId, telefono, "", linkTelmed, true, "", "JOB");
                                    Logger.GetInstance().AddLog(true, "CallTelmedLinks", string.Format("TelemedicinaLinkId {0}", rowInc["IncidenteId"].ToString()));
                                }
                                else
                                {
                                    telmedLinks.SetEnviado(Convert.ToDecimal(rowInc["IncidenteId"]), metodoId, telefono, "", linkTelmed, false, "Error al enviar WA", "JOB");
                                    Logger.GetInstance().AddLog(false, "CallTelmedLinks", string.Format("TelemedicinaLinkId {0}", rowInc["IncidenteId"].ToString()));
                                }
                            }
                            else
                            {
                                telmedLinks.SetEnviado(Convert.ToDecimal(rowInc["IncidenteId"]), metodoId, telefono, "", linkTelmed, false, "Error al enviar WA", "JOB");
                                Logger.GetInstance().AddLog(false, "CallTelmedLinks", string.Format("TelemedicinaLinkId {0}", rowInc["IncidenteId"].ToString()));
                            }

                        }

                        else

                        {

                            telmedLinks.SetEnviado(Convert.ToDecimal(rowInc["IncidenteId"]), 1, 0, "", "", false, "No se pudo generar el link", "JOB");
                            Logger.GetInstance().AddLog(false, "CallTelmedLinks", string.Format("TelemedicinaLinkId {0}", rowInc["IncidenteId"].ToString()));

                        }


                    }
                    catch (Exception ex)
                    {
                        telmedLinks.SetEnviado(Convert.ToDecimal(rowInc["IncidenteId"]), 1, 0, "", "", false, "No se pudo generar el link", "JOB");
                        Logger.GetInstance().AddLog(false, "CallTelmedLinks", string.Format("TelemedicinaLinkId {0} - Error {1}", rowInc["IncidenteId"].ToString(), ex.Message));
                    }
                }
            }
            else
            {
                Logger.GetInstance().AddLog(true, "CallTelmedLinks", "No hay links para enviar");
            }

            /// Establezo los expirados...

            long expired = telmedLinks.SetExpirados();

            Logger.GetInstance().AddLog(true, "CallTelmedLinks", string.Format("Se expiraron {0} links", expired.ToString()));

        }

        private string DateToSQL(DateTime fecha)
        {
            return string.Format("{0}-{1}-{2}", fecha.Year.ToString(), fecha.Month.ToString("00"), fecha.Day.ToString("00"));
        }


        public void CallSyncSQL()
        {

            try
            {
                /// Connect Cache
                ConnectionStringCache cnnCache = this.getConnectionString();

                Logger.GetInstance().AddLog(true, "CallSyncSQL", "Buscando viaje de móviles");

                EmergencyC.MovilesActuales objMovilesOperativos = new EmergencyC.MovilesActuales(cnnCache);
                DataTable dtViajes = objMovilesOperativos.GetViajesMoviles();

                string wSepDecimal = modNumbers.getWSepDecimal();

                SqlConnection sqlcon = new SqlConnection(ConfigurationManager.AppSettings["SQLShamanCache"].ToString());

                sqlcon.Open();

                string SQL = "UPDATE ViajesMoviles SET flgPurge = 1";

                using (SqlCommand cmd = new SqlCommand(SQL, sqlcon))
                {
                    cmd.ExecuteNonQuery();
                }


                for (int i = 0; i < dtViajes.Rows.Count; i++)
                {


                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_SetViajeMovil", sqlcon))
                        {

                            decimal latitud = Convert.ToDecimal(dtViajes.Rows[i]["Latitud"].ToString().Replace(".", wSepDecimal));
                            decimal longitud = Convert.ToDecimal(dtViajes.Rows[i]["Longitud"].ToString().Replace(".", wSepDecimal));

                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add(new SqlParameter("@MovilId", dtViajes.Rows[i]["MovilId"].ToString()));
                            cmd.Parameters.Add(new SqlParameter("@flgPreasignado", Convert.ToInt16(dtViajes.Rows[i]["flgPreasignado"])));
                            cmd.Parameters.Add(new SqlParameter("@IdServicio", Convert.ToInt64(dtViajes.Rows[i]["IdServicio"])));
                            cmd.Parameters.Add(new SqlParameter("@Grado", dtViajes.Rows[i]["Grado"].ToString()));
                            cmd.Parameters.Add(new SqlParameter("@NroServicio", dtViajes.Rows[i]["NroServicio"].ToString()));
                            cmd.Parameters.Add(new SqlParameter("@Cliente", dtViajes.Rows[i]["Cliente"].ToString()));
                            cmd.Parameters.Add(new SqlParameter("@Sexo", dtViajes.Rows[i]["Sexo"].ToString()));
                            cmd.Parameters.Add(new SqlParameter("@Edad", Convert.ToInt32(dtViajes.Rows[i]["Edad"])));
                            cmd.Parameters.Add(new SqlParameter("@Horario", dtViajes.Rows[i]["Horario"].ToString()));
                            cmd.Parameters.Add(new SqlParameter("@Domicilio", dtViajes.Rows[i]["Domicilio"].ToString()));
                            cmd.Parameters.Add(new SqlParameter("@Localidad", dtViajes.Rows[i]["Localidad"].ToString()));
                            cmd.Parameters.Add(new SqlParameter("@Latitud", latitud));
                            cmd.Parameters.Add(new SqlParameter("@Longitud", longitud));
                            cmd.Parameters.Add(new SqlParameter("@ColorHexa", dtViajes.Rows[i]["ColorHexa"].ToString()));
                            cmd.Parameters.Add(new SqlParameter("@CurrentViaje", Convert.ToInt16(dtViajes.Rows[i]["CurrentViaje"])));
                            cmd.Parameters.Add(new SqlParameter("@horLlamada", dtViajes.Rows[i]["horLlamada"].ToString()));

                            cmd.ExecuteNonQuery();

                        }

                    }
                    catch (Exception ex)
                    {
                        Logger.GetInstance().AddLog(true, "CallSyncSQL - Error {0}", ex.Message);
                    }

                }

                SQL = "DELETE FROM ViajesMoviles WHERE flgPurge = 1";

                using (SqlCommand cmd = new SqlCommand(SQL, sqlcon))
                {
                    cmd.ExecuteNonQuery();
                }

                /// Detalle ++

                for (int i = 0; i < dtViajes.Rows.Count; i++)
                {

                    DataTable dtDetalle = objMovilesOperativos.GetViaje(dtViajes.Rows[i]["MovilId"].ToString(), Convert.ToDecimal(dtViajes.Rows[i]["IdServicio"]));

                    if (dtDetalle.Rows.Count > 0)

                    {

                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("sp_SetViajeMovilDetalle", sqlcon))
                            {

                                decimal latitud = Convert.ToDecimal(dtDetalle.Rows[0]["DerLatitud"].ToString().Replace(".", wSepDecimal));
                                decimal longitud = Convert.ToDecimal(dtDetalle.Rows[0]["DerLongitud"].ToString().Replace(".", wSepDecimal));

                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add(new SqlParameter("@MovilId", dtViajes.Rows[i]["MovilId"].ToString()));
                                cmd.Parameters.Add(new SqlParameter("@IdServicio", Convert.ToInt64(dtViajes.Rows[i]["IdServicio"])));
                                cmd.Parameters.Add(new SqlParameter("@fecIncidente", modFechasCs.NtoD(Convert.ToInt64(dtDetalle.Rows[0]["FecIncidente"]))));
                                cmd.Parameters.Add(new SqlParameter("@nroAfiliado", dtDetalle.Rows[0]["nroAfiliado"]));
                                cmd.Parameters.Add(new SqlParameter("@planAfiliado", dtDetalle.Rows[0]["Plan"]));
                                cmd.Parameters.Add(new SqlParameter("@paciente", dtDetalle.Rows[0]["paciente"]));
                                cmd.Parameters.Add(new SqlParameter("@sintomas", dtDetalle.Rows[0]["sintomas"]));
                                cmd.Parameters.Add(new SqlParameter("@copago", dtDetalle.Rows[0]["copago"]));
                                cmd.Parameters.Add(new SqlParameter("@localidad", dtDetalle.Rows[0]["Localidad"]));
                                cmd.Parameters.Add(new SqlParameter("@partido", dtDetalle.Rows[0]["partido"]));
                                cmd.Parameters.Add(new SqlParameter("@entrecalle1", dtDetalle.Rows[0]["entrecalle1"]));
                                cmd.Parameters.Add(new SqlParameter("@entrecalle2", dtDetalle.Rows[0]["entrecalle2"]));
                                cmd.Parameters.Add(new SqlParameter("@referencia", dtDetalle.Rows[0]["referencia"]));
                                cmd.Parameters.Add(new SqlParameter("@telefono", dtDetalle.Rows[0]["telefono"]));
                                cmd.Parameters.Add(new SqlParameter("@observaciones", dtDetalle.Rows[0]["observaciones"]));
                                cmd.Parameters.Add(new SqlParameter("@HabSalida", dtDetalle.Rows[0]["HabSalida"]));
                                cmd.Parameters.Add(new SqlParameter("@HabLlegada", dtDetalle.Rows[0]["HabLlegada"]));
                                cmd.Parameters.Add(new SqlParameter("@HabFinal", dtDetalle.Rows[0]["HabFinal"]));
                                cmd.Parameters.Add(new SqlParameter("@HabCancelacion", dtDetalle.Rows[0]["HabCancelacion"]));
                                cmd.Parameters.Add(new SqlParameter("@clasificacionId", dtDetalle.Rows[0]["clasificacionId"]));
                                cmd.Parameters.Add(new SqlParameter("@institucion", dtDetalle.Rows[0]["institucion"]));
                                cmd.Parameters.Add(new SqlParameter("@diagnostico", dtDetalle.Rows[0]["diagnostico"]));
                                cmd.Parameters.Add(new SqlParameter("@aviso", dtDetalle.Rows[0]["aviso"]));
                                cmd.Parameters.Add(new SqlParameter("@sintomasItems", dtDetalle.Rows[0]["sintomasItems"]));
                                cmd.Parameters.Add(new SqlParameter("@flgRename", dtDetalle.Rows[0]["flgRename"]));
                                cmd.Parameters.Add(new SqlParameter("@flgDerivacion", dtDetalle.Rows[0]["flgDerivacion"]));
                                cmd.Parameters.Add(new SqlParameter("@DerLocalidad", dtDetalle.Rows[0]["DerLocalidad"]));
                                cmd.Parameters.Add(new SqlParameter("@DerPartido", dtDetalle.Rows[0]["DerPartido"]));
                                cmd.Parameters.Add(new SqlParameter("@DerInstitucion", dtDetalle.Rows[0]["DerInstitucion"]));
                                cmd.Parameters.Add(new SqlParameter("@DerDomicilio", dtDetalle.Rows[0]["DerDomicilio"]));
                                cmd.Parameters.Add(new SqlParameter("@DerEntreCalle1", dtDetalle.Rows[0]["DerEntreCalle1"]));
                                cmd.Parameters.Add(new SqlParameter("@DerEntreCalle2", dtDetalle.Rows[0]["DerEntreCalle2"]));
                                cmd.Parameters.Add(new SqlParameter("@DerLatitud", latitud));
                                cmd.Parameters.Add(new SqlParameter("@DerLongitud", longitud));

                                cmd.ExecuteNonQuery();

                            }

                        }
                        catch (Exception ex)
                        {
                            Logger.GetInstance().AddLog(true, "CallSyncSQL - Error {0}", ex.Message);
                        }

                    }

                }

                sqlcon.Close();
                sqlcon = null;

            }

            catch (Exception ex)
            {
                Logger.GetInstance().AddLog(true, "CallSyncSQL - Error {0}", ex.Message);
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

            Logger.GetInstance().AddLog(true, "CallTeleasistencia", "Buscando Mensajes para Teleasistencia");

            EmergencyC.IncPendientesTeleasistencia incPendientesTeleasistencia = new EmergencyC.IncPendientesTeleasistencia(cnnCache);
            List<EmergencyDTO.TeleasistenciaReq> listTeleasistenciaReq = incPendientesTeleasistencia.GetPendientes();

            if (listTeleasistenciaReq != null)
            {

                Logger.GetInstance().AddLog(true, "CallTeleasistencia", string.Format("Se encontraron {0} mensajes", listTeleasistenciaReq.Count.ToString()));

                foreach (EmergencyDTO.TeleasistenciaReq teleasistenciaReq in listTeleasistenciaReq)
                {

                    long lngConferenceId = 0;

                    SendMethods sm = new SendMethods(ConfigurationManager.AppSettings["AIDShamanAPI_URL"]);

                    try
                    {
                        ConferenceBO oReturn = sm.NewWithUser(teleasistenciaReq, "");

                        if (oReturn != null)
                        {
                            lngConferenceId = oReturn.ConferenceId;
                        }

                        if (lngConferenceId > 0)
                        {
                            if (incPendientesTeleasistencia.SaveConferenceID(teleasistenciaReq.ShamanIncidenteID, lngConferenceId))
                            {
                                Logger.GetInstance().AddLog(true, "CallTeleasistencia", string.Format("IncidenteId {0} - ConferenceId {1}", teleasistenciaReq.ShamanIncidenteID.ToString(), lngConferenceId));
                            }
                            else
                            {
                                Logger.GetInstance().AddLog(false, "CallTeleasistencia", string.Format("IncidenteId {0} - Error al salvar la conferencia", teleasistenciaReq.ShamanIncidenteID.ToString()));
                            }
                        }
                        else
                        {
                            Logger.GetInstance().AddLog(false, "CallTeleasistencia", string.Format("IncidenteId {0} - Devolvión NULL el Método", teleasistenciaReq.ShamanIncidenteID.ToString()));
                            incPendientesTeleasistencia.SaveConferenceID(teleasistenciaReq.ShamanIncidenteID, lngConferenceId, "PACIENTE BLOQUEADO POR EL PORTAL");
                        }

                    }
                    catch (Exception ex)
                    {
                        Logger.GetInstance().AddLog(false, "CallTeleasistencia", string.Format("IncidenteId {0} - Error {1}", teleasistenciaReq.ShamanIncidenteID.ToString(), ex.Message));
                    }
                }
            }
        }

        public void CallGPS()
        {

            try
            {

                /// Connect Cache
                ConnectionStringCache cnnCache = this.getConnectionString();

                string vExe = new PanelC.Perifericos(cnnCache).GetSintaxis(1);

                Logger.GetInstance().AddLog(true, "CallGPS", "Buscando posiciones");

                var client = new WebClient();

                string content = client.DownloadString(vExe);

                string[] moviles = content.Split((char)10);

                for (int i=0; i<moviles.Length; i++)
                {

                    string[] movil = moviles[i].Split('|');

                    EmergencyC.Vehiculos objVehiculos = new EmergencyC.Vehiculos(cnnCache);

                    Debug.Print(movil[0]);

                    if (objVehiculos.Abrir(objVehiculos.GetIdByDominio(movil[0]).ToString()))
                    {

                        CompumapC.GpsActual objGps = new CompumapC.GpsActual(cnnCache);

                        if (objGps.SetPosition(movil[0], objVehiculos.GetMovilId(objVehiculos.ID, DateTime.Now), objVehiculos.ID, movil[2], modNumbers.GetDouble(movil[3], true), modNumbers.GetDouble(movil[4], true),
                            "", movil[6], movil[7], movil[5] != "" ? Convert.ToInt32(Convert.ToDecimal(movil[5])) : 0, movil[8], movil[9], movil[10], "JOB"))
                        {
                            Logger.GetInstance().AddLog(true, "CallGPS", string.Format("Vehículo {0} ok", movil[0]));
                        }
                        else
                        {
                            Logger.GetInstance().AddLog(false, "CallGPS", string.Format("Error al guardar el vehículo {0}", movil[0]));
                        }

                        objGps = null;

                    }
                    else
                    {
                        Logger.GetInstance().AddLog(false, "CallGPS", string.Format("No se pudo vincular el vehículo {0}", movil[0]));
                    }

                    objVehiculos = null;

                }

            }
            catch (Exception ex)
            {
                Logger.GetInstance().AddLog(false, "CallGPS", ex.Message);
            }
        }

        public void CallCronos()
        {

            try
            {

                Logger.GetInstance().AddLog(true, "CallCronos", "Buscando fichadas");

                long fecDesde = modFechasCs.DtoN(DateTime.Now.AddDays(Convert.ToInt32(ConfigurationManager.AppSettings["CronosDays"]) * - 1));

                DataTable dtIngresos = new DataTable();

                using (OleDbConnection connection = new OleDbConnection(ConfigurationManager.AppSettings["CronosSQLAccess"].ToString()))
                {

                    string cmdText = "";

                    cmdText = "SELECT * FROM Fichadas WHERE fichada_fecha >= '" + fecDesde.ToString() + "'";

                    using (OleDbCommand command = new OleDbCommand(cmdText, connection))
                    {
                        command.CommandType = CommandType.Text;
                        using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(dtIngresos);
                        }
                    }
                }

                Logger.GetInstance().AddLog(true, "CallCronos", string.Format("Se encontraron {0} fichadas", dtIngresos.Rows.Count.ToString()));

                if (dtIngresos.Rows.Count > 0)
                {
                    /// Connect Cache
                    ConnectionStringCache cnnCache = this.getConnectionString();

                    for (int i = 0; i < dtIngresos.Rows.Count; i++)
                    {

                        DataRow row = dtIngresos.Rows[i];

                        RrhhC.IngresosDiarios objIngresos = new RrhhC.IngresosDiarios(cnnCache);

                        DevPersistir rdoSave = objIngresos.SetFichadaByDocumento(Convert.ToDecimal(row["fichada_numero"]), Convert.ToDecimal(row["fichada_fecha"]),
                            modFechasCs.MinutesToTime(Convert.ToInt64(row["fichada_hora"])), Convert.ToInt32(row["fichada_evento"]), row["fichada_registrador"].ToString());

                        if (!rdoSave.Resultado)
                        {
                            Logger.GetInstance().AddLog(true, "CallCronos", string.Format("Nro. Documento: {0} / Fecha: {1} / Error: {2} ", row["fichada_numero"].ToString(), row["fichada_fecha"].ToString(), rdoSave.DescripcionError));
                        }

                    }

                    /// Limpio fichadas

                    OleDbConnection sqlcon = new OleDbConnection(ConfigurationManager.AppSettings["CronosSQLAccess"].ToString());

                    sqlcon.Open();

                    string SQL = "DELETE FROM Fichadas";

                    using (OleDbCommand cmd = new OleDbCommand(SQL, sqlcon))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    sqlcon.Close();
                    sqlcon = null;

                }

            }

            catch (Exception ex)
            {
                Logger.GetInstance().AddLog(true, "CallCronos - Error {0}", ex.Message);
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
