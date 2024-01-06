using System;
using System.Data;
using System.Configuration;
using System.Web;

using System.Diagnostics;                   // EventLog
using System.Xml;							// XmlDocument
using System.Collections.Generic;			// List
using System.Collections.Specialized;       // NameValueCollection
using System.Net.Mail;                      // For sending email via SMTP
using System.Data.SqlClient;				// For SQLConnection, et al.

using PGNamespace;
using Newtonsoft.Json.Linq;					// For JObject
using System.Web.Configuration;
using System.Net;
using System.Text;							// For class Encoding
using System.IO;							// For StreamReader


namespace PortalGuard {
	public class NewUserStagingHandler : IHttpHandler  {
		// Member constants
		public const string PGAPI_RC_FAILURE			= "63";
		public const string PGAPI_RC_NOERROR			= "0";
		public const string FRMFLD_FIRSTNAME 			= "Firstname";
		public const string FRMFLD_LASTNAME 			= "Lastname";
		public const string FRMFLD_EMAIL	 			= "Email";
		public const string FRMFLD_PHONE	 			= "Phone";
		public const string FRMFLD_CAMPUS	 			= "Campus";
		public const string FRMFLD_PROGRAM	 			= "Program";
		public const string FRMFLD_STUDENTID 			= "studentid";

		
        // Member variables
		private List<PGError> errors;

		
		public void ProcessRequest(HttpContext context) {
			String xml = "";
			HttpRequest req = context.Request;
			HttpResponse resp = context.Response;

			errors = new List<PGError>();
    
			try {
				// 0) Ensure it's a POST request
				if (!Utilities.isPOST(req)) {
					errors.Add(new PGError(PGError.VLDERR.INTERNAL_ERROR, "Only POST requests are supported"));
					resp.Write(BuildReturnXML());
					return;
				}

				// 1) Authenticate the request
				string secret = Utilities.getAppSetting("SharedSecret");
				string fromreq = (req.Headers["Authorization"]);
				if (null == fromreq)
					fromreq = "";
				fromreq = Utilities.StrRight(fromreq, "Basic ");
				if (0 != String.Compare(secret, fromreq)) {
					errors.Add(new PGError(PGError.VLDERR.BAD_PASSWORD, "Missing or invalid Authorization header"));
					resp.Write(BuildReturnXML());
					return;
				}

				// 2) First-level validation of input (anything missing?)
				if (!isInputValid(req)) {
					resp.Write(BuildReturnXML());
					return;
				}

				// 3) Generate random token
				string token = System.Guid.NewGuid().ToString();

				// 4) Stage new data in SQL
				if (0 != Utilities.stageRegistration(token, req.Form)) {
					errors.Add(new PGError(PGError.VLDERR.INTERNAL_ERROR, "SQLStaging"));
					resp.Write(BuildReturnXML());
					return;
				}
				
				// 5) Send invitation email to end-user
				string subj = Utilities.getAppSetting("Staging_EmailSubj");
				string body = Utilities.getTemplateFileContents(Utilities.getAppSetting("Staging_EmailBody_TemplateFile"));
				body = body.Replace("{RANDTOKEN}", token);

				if (0 != Utilities.sendEmail(req.Form[FRMFLD_EMAIL], Utilities.subParams(subj, req), Utilities.subParams(body, req))) {
					errors.Add(new PGError(PGError.VLDERR.BAD_EMAIL_SYNTAX, FRMFLD_EMAIL));
					resp.Write(BuildReturnXML());
					return;
				}

				// Success - no errors
				xml = BuildReturnXML();

			} catch (Exception e) {
				xml = Utilities.BuildErrorXML(e);
			}
			resp.Write(xml);
		}


        private bool isInputValid(HttpRequest req) {
            // Loop over all fields that shouldn't be blank
            string[] nonblankfields = new string[] { FRMFLD_FIRSTNAME, FRMFLD_LASTNAME, FRMFLD_EMAIL, FRMFLD_PHONE, FRMFLD_CAMPUS, FRMFLD_STUDENTID };
            foreach (string fld in nonblankfields) {
                if (isNullOrBlank(req, fld, true)) { errors.Add(new PGError(PGError.VLDERR.BLANK, fld)); }
            }

            if (errors.Count > 0) {
                return false;
            }

            return true;
        }

		
        public bool IsReusable {
			get { return false; }
		}

	
		private bool isNullOrBlank(HttpRequest req, string fld, bool bTrimWhiteSpace) {
			if (String.IsNullOrEmpty(req.Form[fld])) {
				return true;
			} else {
				if (bTrimWhiteSpace) {
					string trimmed = req.Form[fld].Trim();
					if (0 == trimmed.Length) {
						return true;
					} else {
						return false;
					}
				} else {
					return false;
				}
			}
		}


		private string BuildReturnXML() {
			XmlDocument xdoc = new XmlDocument();
			XmlNode node = xdoc.CreateElement("pg_return");
			xdoc.AppendChild(node);

			// Top-level error code
			node = xdoc.CreateElement("maj_error");
			if (errors.Count > 0) {
				node.InnerText = PGAPI_RC_FAILURE;
				xdoc.FirstChild.AppendChild(node);

				XmlNode errs_node = xdoc.CreateElement("errs");
				for (int i = 0; i < errors.Count; i++) {
					XmlNode errnode = xdoc.ImportNode((errors[i]).ToXML(), true);
					errs_node.AppendChild(errnode);
				}
				xdoc.FirstChild.AppendChild(errs_node);
			} else {
				node.InnerText = PGAPI_RC_NOERROR;
				xdoc.FirstChild.AppendChild(node);
			}			

			return xdoc.InnerXml;
		}
	}


	public class Utilities {
		public static bool isGET(HttpRequest req) { return (0 == String.Compare(req.HttpMethod, "GET", true)); }
		public static bool isPOST(HttpRequest req) { return (0 == String.Compare(req.HttpMethod, "POST", true)); }

		public static string StrLeft(string inp, string fnd) {
			int pos = inp.IndexOf(fnd);
			if (pos != -1) {
				return inp.Substring(0, pos);
			}
			return "";
		}

		public static string StrRight(string inp, string fnd) {
			int pos = inp.IndexOf(fnd);
			if (pos != -1) {
				return inp.Substring(pos + fnd.Length);
			}
			return "";
		}

		public static bool StrContains(string inp, string find, bool bNoCase) {
			int pos = -1;
			if (bNoCase) {
				pos = inp.ToLower().IndexOf(find.ToLower());
			} else {
				pos = inp.IndexOf(find);
			}
			if (pos >= 0)
				return true;
			return false;
		}

		public static string BuildErrorXML(Exception e) {
			return BuildErrorXML(e.Message);
		}

		public static string BuildErrorXML(string msg) {
			XmlDocument xdoc = new XmlDocument();
			XmlNode node = xdoc.CreateElement("pg_return");
			xdoc.AppendChild(node);

			// Top-level error code
			node = xdoc.CreateElement("maj_error");
			node.InnerText = NewUserStagingHandler.PGAPI_RC_FAILURE;
			xdoc.FirstChild.AppendChild(node);

			node = xdoc.CreateElement("err_desc");
			node.InnerText = msg;
			xdoc.FirstChild.AppendChild(node);

			return xdoc.InnerXml;
		}

		public static string getTemplateFileContents(string nm) {
			string path = @"C:\Program Files\PistolStar\PortalGuard\Policies";
			path = System.IO.Path.Combine(path, nm);
			try {
				return System.IO.File.ReadAllText(path);
			} catch (Exception ex) {
				LogError(String.Format("{0} - Caught exception: {1}", "Pacific-SelfReg-TA.ashx.cs::getTemplateFileContents()", ex.Message));
			}
			return "";
		}

		public static string subParams(string template, HttpRequest req) {
			NameValueCollection frm = req.Form;
			string ret = template;
			for (int i = 0; i < frm.AllKeys.Length; i++) {
				string nm = (string)frm.AllKeys.GetValue(i);
				string caps = nm.ToUpper();
				ret = ret.Replace("{" + caps + "}", frm[nm]);
			}

			// 2021-10-25 - One that we always need to replace!
			string pgserv = String.Format("http{0}://{1}", (req.IsSecureConnection ? "s" : ""), req.ServerVariables["HTTP_HOST"]);
			ret = ret.Replace("{PGSERVER}", pgserv);

			return ret;
		}

		public static string getAppSetting(string nm) { return WebConfigurationManager.AppSettings[nm]; }

		public static string getSQLConnStr() {
			try {
				return ConfigurationManager.ConnectionStrings["connDefault"].ConnectionString;
			} catch (Exception ex) {
				LogError(String.Format("{0} - Caught exception: {1}", "Pacific-SelfReg-TA.ashx.cs::getSQLConnStr()", ex.Message));
			}
			return "";
		}


		public static int stageRegistration(string token, NameValueCollection form) {
			int nRet = 1;       // Error by default
			string strConn = getSQLConnStr();

			try {
				using (SqlConnection conn = new SqlConnection(strConn)) {
					using (SqlCommand cmd = new SqlCommand("createPacificCollegeRegistration", conn)) {
						cmd.CommandType = CommandType.StoredProcedure;
						cmd.Parameters.Add("@randtoken", SqlDbType.VarChar, 256).Value = token;
						cmd.Parameters.Add("@firstname", SqlDbType.VarChar, 256).Value = form[NewUserStagingHandler.FRMFLD_FIRSTNAME];
						cmd.Parameters.Add("@lastname", SqlDbType.VarChar, 256).Value = form[NewUserStagingHandler.FRMFLD_LASTNAME];
						cmd.Parameters.Add("@email", SqlDbType.VarChar, 256).Value = form[NewUserStagingHandler.FRMFLD_EMAIL];
						cmd.Parameters.Add("@phone", SqlDbType.VarChar, 256).Value = form[NewUserStagingHandler.FRMFLD_PHONE];
						cmd.Parameters.Add("@campus", SqlDbType.VarChar, 256).Value = form[NewUserStagingHandler.FRMFLD_CAMPUS];
						//cmd.Parameters.Add("@program", SqlDbType.VarChar, 256).Value = form[NewUserStagingHandler.FRMFLD_PROGRAM];
						cmd.Parameters.Add("@studentid", SqlDbType.VarChar, 256).Value = form[NewUserStagingHandler.FRMFLD_STUDENTID];
						cmd.Parameters.Add("@errnum", SqlDbType.Int, 0);
						cmd.Parameters["@errnum"].Direction = ParameterDirection.Output;
						cmd.Parameters.Add("@errstr", SqlDbType.VarChar, 256);
						cmd.Parameters["@errstr"].Direction = ParameterDirection.Output;

						conn.Open();
						Int32 rowsAffected = cmd.ExecuteNonQuery();
						nRet = 0;
					}
				}
			} catch (Exception ex) {
				LogError(String.Format("{0} - Caught exception: {1}", "Pacific-SelfReg-TA.ashx.cs::stageRegistration()", ex.Message));
			}

			return nRet;
		}


		public static int getRegistration(string token, ref Dictionary<string, string> fields, ref string err) {
			int nRet = 1;       // Error by default
			string strConn = getSQLConnStr();

			if (null == fields)
				fields = new Dictionary<string, string>();
			else
				fields.Clear();

			try {
				using (SqlConnection conn = new SqlConnection(strConn)) {
					using (SqlCommand cmd = new SqlCommand("getPacificCollegeRegistration", conn)) {
						cmd.CommandType = CommandType.StoredProcedure;
						cmd.Parameters.Add("@randtoken", SqlDbType.VarChar, 256).Value = token;
						cmd.Parameters.Add("@errnum", SqlDbType.Int, 0);
						cmd.Parameters["@errnum"].Direction = ParameterDirection.Output;
						cmd.Parameters.Add("@errstr", SqlDbType.VarChar, 256);
						cmd.Parameters["@errstr"].Direction = ParameterDirection.Output;

						conn.Open();
						using (SqlDataReader rdr = cmd.ExecuteReader()) {
							if (rdr.Read()) {
								for (int i = 0; i < rdr.FieldCount; i++) {
									if (typeof(string) == rdr.GetValue(i).GetType())
										fields[rdr.GetName(i)] = rdr.GetString(i);
								}
							} else {
								return nRet;    // No results!
							}
						}

						int errnum = (int)cmd.Parameters["@errnum"].Value;
						string errstr = (string)cmd.Parameters["@errstr"].Value;
						err = String.Format("Return value: {0}: {1}", errnum, errstr);
						nRet = 0;
					}
				}
			} catch (Exception ex) {
				LogError(String.Format("{0} - Caught exception: {1}", "Pacific-SelfReg-TA.ashx.cs::getRegistration()", ex.Message));
			}

			return nRet;
		}


		public static int completeRegistration(string token, string username) {
			int nRet = 1;       // Error by default
			string strConn = getSQLConnStr();

			try {
				using (SqlConnection conn = new SqlConnection(strConn)) {
					using (SqlCommand cmd = new SqlCommand("updatePacificCollegeRegistration", conn)) {
						cmd.CommandType = CommandType.StoredProcedure;
						cmd.Parameters.Add("@randtoken", SqlDbType.VarChar, 256).Value = token;
						cmd.Parameters.Add("@username", SqlDbType.VarChar, 256).Value = username;
						cmd.Parameters.Add("@errnum", SqlDbType.Int, 0);
						cmd.Parameters["@errnum"].Direction = ParameterDirection.Output;
						cmd.Parameters.Add("@errstr", SqlDbType.VarChar, 256);
						cmd.Parameters["@errstr"].Direction = ParameterDirection.Output;

						conn.Open();
						Int32 rowsAffected = cmd.ExecuteNonQuery();
						nRet = 0;
					}
				}
			} catch (Exception ex) {
				LogError(String.Format("{0} - Caught exception: {1}", "Pacific-SelfReg-TA.ashx.cs::completeRegistration()", ex.Message));
			}

			return nRet;
		}


		public static void LogError(String msg) { LogEvent(msg, EventLogEntryType.Error, 32); }
		public static void LogInfo(String msg) { LogEvent(msg, EventLogEntryType.Information, 0); }
		public static void LogEvent(String msg, EventLogEntryType t, int code) {
			EventLog log = new EventLog();
			log.Source = "PortalGuard";
			log.WriteEntry(msg, t, code);
		}


		public static int sendEmail(string to, string subject, string body) {
			// Settings are read from web.config: http://weblogs.asp.net/scottgu/432854
			using (SmtpClient client = new SmtpClient()) {
				MailMessage message = new MailMessage();
				string cleaned = to.Trim();
				if (!String.IsNullOrEmpty(cleaned)) {
					message.To.Add(new MailAddress(cleaned));
				}

				// Exit immediatley if no recipients are defined...
				if (0 == message.To.Count)
					return 0;

				message.Subject = subject;
				message.Body = body;
				message.BodyEncoding = System.Text.UTF8Encoding.UTF8;
				message.IsBodyHtml = true;

				try {
					client.Send(message);
				} catch (Exception ex) {
					LogError(String.Format("{0} - Caught exception: {1}", "Pacific-SelfReg-TA.ashx.cs::sendEmail()", ex.Message));
					return 1;
				}
			}   // "using" stmt automatically calls Dispose - MUST use .NET v4.0!!

			return 0;
		}


		public static string doHTTPPOST(string url, Dictionary<string, string> POSTvalues) {
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);

			try {
				string postData = "";
				foreach (string key in POSTvalues.Keys) {
					postData += String.Format("{0}{1}={2}", (postData.Length > 0 ? "&" : ""), Uri.EscapeDataString(key), Uri.EscapeDataString(POSTvalues[key]));
				}
				byte[] data = Encoding.ASCII.GetBytes(postData);

				req.Method = "POST";
				req.ContentType = "application/x-www-form-urlencoded";
				req.ContentLength = data.Length;
				using (var stream = req.GetRequestStream()) {
					stream.Write(data, 0, data.Length);
				}
				HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
				return new StreamReader(resp.GetResponseStream()).ReadToEnd();
			} catch (Exception ex) {
				LogError(String.Format("{0} - Request to '{1}' threw exception: {2}", "Pacific-SelfReg-TA.ashx.cs::doHTTPPOST()", url, ex.Message));
			}
			return "";
		}


		public static string doHTTPGET(string url, Dictionary<string, string> headers) {
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);

			try {
				foreach (string key in headers.Keys) {
					req.Headers.Add(key, headers[key]);
				}
				HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
				return new StreamReader(resp.GetResponseStream()).ReadToEnd();
			} catch (Exception ex) {
				LogError(String.Format("{0} - Request to '{1}' threw exception: {2}", "Pacific-SelfReg-TA.ashx.cs::doHTTPPOST()", url, ex.Message));
			}
			return "";
		}


		public static bool existsInAzureAD(string uname) {
			string mod = "Pacific-SelfReg-TA.ashx.cs::existsInAzureAD()";
			string tenantId = getAppSetting("AADTenantID");
			string clientId = getAppSetting("AADClientID");
			string theuser = getAppSetting("AADUser");
			string thepass = getAppSetting("AADPassword");

			if (String.IsNullOrEmpty(tenantId) || String.IsNullOrEmpty(clientId) || String.IsNullOrEmpty(theuser) || String.IsNullOrEmpty(thepass)) {
				LogError(String.Format("{0} - A required application setting was blank in web.config", mod));
				return true;
			}

			LogInfo(String.Format("{0} - Calling Azure AD to lookup choosen self-registration username: {1}", mod, uname));

			try {
				// Get JWT first
				Dictionary<string, string> values = new Dictionary<string, string>();
				values.Add("client_id", clientId);
				values.Add("resource", "https://graph.windows.net");
				values.Add("grant_type", "password");
				values.Add("scope", "openid");
				values.Add("username", theuser);
				values.Add("password", thepass);
				string url = String.Format("https://login.microsoftonline.com/{0}/oauth2/token", tenantId);
				string jwt = doHTTPPOST(url, values);
				if (String.IsNullOrEmpty(jwt)) {
					LogError(String.Format("{0} - Malformed POST response to retrieve JWT for Azure AD access", mod));
					return true;
				}

				// Parse out the access_token
				JObject o = JObject.Parse(jwt);
				string access_token = (string)o.SelectToken("access_token");
				if (String.IsNullOrEmpty(access_token)) {
					LogError(String.Format("{0} - Failed to retrieve JWT for Azure AD access", mod));
					return true;
				}

				// Now search for the user with this JWT as authentication
				url = String.Format("https://graph.windows.net/{0}/users?api-version=1.6&$filter=mailNickname%20eq%20'{1}'", tenantId, HttpUtility.UrlEncode(uname));
				values.Clear();
				values.Add("Authorization", String.Format("Bearer {0}", access_token));
				string res = doHTTPGET(url, values);
				o = JObject.Parse(res);
				string userresult = (string)o.SelectToken("value[0].mailNickname");
				if (!String.IsNullOrEmpty(userresult)) {
					LogInfo(String.Format("{0} - Found existing user '{1}' in Azure AD", mod, uname));
					return true;
				}

				return false;

			} catch (Exception ex) {
				LogError(String.Format("{0} - Caught exception: {1}", mod, ex.Message));
				return true;
			}
		}
	}
}
