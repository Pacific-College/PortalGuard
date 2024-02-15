using System;
using System.Data;
using System.Configuration;
using System.Web;

using System.Xml;							// XmlDocument
using System.Collections.Generic;			// List
using System.Collections.Specialized;		// NameValueCollection
using System.Text.RegularExpressions;		// Regex
using System.Data.SqlClient;				// For SQLConnection, et al.

using System.Web.SessionState;				// 2014-08-13 - For sessions

using PGNamespace;


namespace PortalGuard {
	public class RegistrationHandler : IHttpHandler, IRequiresSessionState  {
		// Member constants
		private const string PGAPI_RC_SELFREG_FAILURE				= "63";
		private const string PGAPI_RC_SELFREG_SUCCESS				= "64";
        private const string PGTOKEN_NAME                           = "PGToken";
		private const int PGAPI_RC_NOERROR							= 0;
        private const int PGAPI_RC_CAUGHT_EXCEPTION                 = 32;
		

        // Member variables
		private List<PGError> errors;


		public void ProcessRequest(HttpContext context) {
			String xml = "";
			HttpRequest req = context.Request;
			HttpResponse resp = context.Response;

			errors = new List<PGError>();

            // 2015-02-04 - Anti-CSRF protection now, expect the PGToken form value to match the PGToken cookie value (both must be present!)
            bool    bCSRF = true;   // Guilty until proven innocent...
            HttpCookie cook = req.Cookies[PGTOKEN_NAME];
            if (null != cook && !String.IsNullOrEmpty(cook.Value)) {
                // Now get the form field value
                string formval = req.Form[PGTOKEN_NAME];
                if (!String.IsNullOrEmpty(formval)) {
                    if (0 == formval.CompareTo(cook.Value)) {
                        bCSRF = false;
                    }
                }
            }
            if (bCSRF) {
                errors.Add(new PGError(PGError.VLDERR.INTERNAL_ERROR, ""));
                resp.Write(BuildReturnXML());
                return;
            }
		    
			try {
                // Pacific College TA - Ensure token is legitimate
                if (String.IsNullOrEmpty(req.Form["randtoken"])) {
                    resp.Write(PortalGuard.Utilities.BuildErrorXML("Save failed - Invalid token"));
                    return;
                }

                Dictionary<string, string> fromsql = null;
                string sqlerr = "";
                if (0 != PortalGuard.Utilities.getRegistration(req.Form["randtoken"], ref fromsql, ref sqlerr) || 0 == fromsql.Count) {
                    if (0 == fromsql.Count)
                        sqlerr = "Unknown token";
                    resp.Write(PortalGuard.Utilities.BuildErrorXML(String.Format("Save failed - {0}", sqlerr)));
                    return;
                }

                // 2021-10-27 - The RandomToken, Username and the Password are the only fields we want to grab from the request
                //  Everything else should be pulled from SQL since we can't otherwise "trust" data sent from the browser
                foreach (string key in fromsql.Keys) {
                    SetFormField(ref req, key, fromsql[key]);
                }

                // First-level validation done here
                if (!isInputValid(req)) {
					resp.Write(BuildReturnXML());
					return;
				}
				
				// 1) CAPTCHA validation (if enabled)
                if (0 != PreCheckCAPTCHA(ref req, ref resp)) {
                    resp.Write(BuildReturnXML());
                    return;
                }
                xml = PGNamespace.SelfReg.CheckCAPTCHA(ref req, ref resp);
                if (!wasPGSuccessful(xml)) {
                    resp.Write(xml);
                    return;
                }
                if (0 != PostCheckCAPTCHA(ref req, ref resp)) {
                    resp.Write(BuildReturnXML());
                    return;
                }

                // 2) Username uniqueness check (if enabled)
                if (0 != PreCheckUsername(ref req, ref resp)) {
                    resp.Write(BuildReturnXML());
                    return;
                }
                xml = PGNamespace.SelfReg.CheckUsername(ref req, ref resp);
                if (!wasPGSuccessful(xml)) {
                    resp.Write(xml);
                    return;
                }
                if (0 != PostCheckUsername(ref req, ref resp)) {
                    resp.Write(BuildReturnXML());
                    return;
                }

                // 3) Email uniqueness check (if enabled)
                if (0 != PreCheckEmail(ref req, ref resp)) {
                    resp.Write(BuildReturnXML());
                    return;
                }
                xml = PGNamespace.SelfReg.CheckEmail(ref req, ref resp);
                if (!wasPGSuccessful(xml)) {
                    resp.Write(xml);
                    return;
                }
                if (0 != PostCheckEmail(ref req, ref resp)) {
                    resp.Write(BuildReturnXML());
                    return;
                }

                // 4) Password quality check
                if (0 != PreCheckPassword(ref req, ref resp)) {
                    resp.Write(BuildReturnXML());
                    return;
                }
                xml = PGNamespace.SelfReg.CheckPassword(ref req, ref resp);
                if (!wasPGSuccessful(xml)) {
                    resp.Write(xml);
                    return;
                }
                if (0 != PostCheckPassword(ref req, ref resp)) {
                    resp.Write(BuildReturnXML());
                    return;
                }

                // 5) Create/stage user account
                if (0 != PreCreateUserAccount(ref req, ref resp)) {
                    //resp.Write(BuildReturnXML());
                    return;
                }
                xml = PGNamespace.SelfReg.CreateUser(ref req, ref resp);
                if (!wasPGSuccessful(xml)) {
                    resp.Write(xml);
                    return;
                }
                if (0 != PostCreateUserAccount(ref req, ref resp)) {
                    //resp.Write(BuildReturnXML());
                    return;
                }
		
				// Optional custom code to run when registration is successful can be added here
				//doSQLActions(req.Form);
			} catch (Exception e) {
				xml = BuildErrorXML(e);
			}
			resp.Write(xml);
		}



        /*****************************************************************
         * START OF SUGGESTED CODE TO CUSTOMIZE
         *****************************************************************/
        private int PreCheckCAPTCHA(ref HttpRequest req, ref HttpResponse resp) {
            //System.Diagnostics.Debugger.Break();
            /*try {
                SetCookie(ref req, "NewCookie", "New-cookie-value");
                SetFormField(ref req, "NewField", "New-field-value");
                SetHeader(ref req, "NewHeader", "New-header-value");
                SetQueryString(ref req, "NewQS", "New-QS-value");
            } catch (Exception) {
                errors.Add(new PGError(PGError.VLDERR.BAD_CAPTCHA, ""));
                return PGAPI_RC_CAUGHT_EXCEPTION;
            }*/
            return PGAPI_RC_NOERROR;    // Successful
        }
        private int PostCheckCAPTCHA(ref HttpRequest req, ref HttpResponse resp) {
            return PGAPI_RC_NOERROR;
        }

        private int PreCheckUsername(ref HttpRequest req, ref HttpResponse resp) {
            return PGAPI_RC_NOERROR;
        }
        private int PostCheckUsername(ref HttpRequest req, ref HttpResponse resp) {

            // TODO - Ensure username is available in Azure AD

            return PGAPI_RC_NOERROR;
        }

        private int PreCheckEmail(ref HttpRequest req, ref HttpResponse resp) {
            return PGAPI_RC_NOERROR;
        }
        private int PostCheckEmail(ref HttpRequest req, ref HttpResponse resp) {
            return PGAPI_RC_NOERROR;
        }

        private int PreCheckPassword(ref HttpRequest req, ref HttpResponse resp) {
            return PGAPI_RC_NOERROR;
        }
        private int PostCheckPassword(ref HttpRequest req, ref HttpResponse resp) {
            return PGAPI_RC_NOERROR;
        }

        private int PreCreateUserAccount(ref HttpRequest req, ref HttpResponse resp) {
            // Ensure the selected username isn't in Azure AD!
            if (Utilities.existsInAzureAD(req.Form["Username"])) {
                errors.Add(new PGError(PGError.VLDERR.UNUSABLE_USERNAME_AAD, "Username"));
                //resp.Write(PortalGuard.Utilities.BuildErrorXML("Username exists in Azure AD"));
                resp.Write(BuildReturnXML());
                return 1;
            }

          string baseDN = ",OU=Groups,DC=office,DC=pacificcollege,DC=edu";
			
            // Map the "campus" to the proper AD group (must be the full DN of the target group!)
            if (0 == String.Compare(req.Form["campus"], "chicago", true)) {
                SetFormField(ref req, "Group1", String.Format("CN=Students-CHI,OU=Distro Groups{0}", baseDN));
				SetFormField(ref req, "Group2", String.Format("CN=SP_Students_CHI,OU=SharePoint,OU=Security Groups{0}", baseDN));
				SetFormField(ref req, "Group3", String.Format("CN=CHI PaperCut Student,OU=Chicago,OU=Security Groups{0}", baseDN));
            } else if (0 == String.Compare(req.Form["campus"], "new york", true)) {
                SetFormField(ref req, "Group1", String.Format("CN=Students-NY,OU=Distro Groups{0}", baseDN));
				SetFormField(ref req, "Group2", String.Format("CN=SP_Students_NY,OU=SharePoint,OU=Security Groups{0}", baseDN));
				SetFormField(ref req, "Group3", String.Format("CN=NY PaperCut Student,OU=New York,OU=Security Groups{0}", baseDN));
            } else if (0 == String.Compare(req.Form["campus"], "san diego", true)) {
                SetFormField(ref req, "Group1", String.Format("CN=Students-SD,OU=Distro Groups{0}", baseDN));
				SetFormField(ref req, "Group2", String.Format("CN=SP_Students_SD,OU=SharePoint,OU=Security Groups{0}", baseDN));
				SetFormField(ref req, "Group3", String.Format("CN=SD PaperCut Student,OU=San Diego,OU=Security Groups{0}", baseDN));
            } else if (0 == String.Compare(req.Form["campus"], "online", true)) {
                SetFormField(ref req, "Group1", String.Format("CN=Students-OL,OU=Distro Groups{0}", baseDN));
				SetFormField(ref req, "Group2", String.Format("CN=SP_Students_OL,OU=SharePoint,OU=Security Groups{0}", baseDN));
				// 2021-11-19 - Blank groups aren't attempted!
				SetFormField(ref req, "Group3", "");
            }
            return PGAPI_RC_NOERROR;
        }
		
        private int PostCreateUserAccount(ref HttpRequest req, ref HttpResponse resp) {
            string token = req.Form["randtoken"];
            string username = req.Form["Username"];
            if (0 != Utilities.completeRegistration(token, username)) {
                resp.Write(PortalGuard.Utilities.BuildErrorXML("Save to SQL failed"));
                return 1;
            }

            /*if (0 != SendSecondaryEmail(ref req, ref resp)) {
                resp.Write(PortalGuard.Utilities.BuildErrorXML("Failed to send secondary email"));
                //return 1; // uncomment if an error sending this email should prevent continuation and result in a failure
            }*/
 
            // 5) Send invitation email to end-user
            /*string subj = Utilities.getAppSetting("Completed_EmailSubj");
            string body = Utilities.getTemplateFileContents(Utilities.getAppSetting("Completed_EmailBody_TemplateFile"));
            //body = body.Replace("{RANDTOKEN}", token);

            if (0 != Utilities.sendEmail(req.Form[NewUserStagingHandler.FRMFLD_EMAIL], Utilities.subParams(subj, req), Utilities.subParams(body, req))) {
                resp.Write(PortalGuard.Utilities.BuildErrorXML("Failed to send confirmation email"));
                return 1;
            }*/

            return PGAPI_RC_NOERROR;
        }
        
        private int SendSecondaryEmail(ref HttpRequest req, ref HttpResponse resp) {
            
            // reading "program" field, field is set from SQL, expected values from SalesForce program field
            string program = req.Form[NewUserStagingHandler.FRMFLD_PROGRAM]; 

            string lms = "moodle";

            program = program.Trim().ToLower();
            
            if (program == "bsn-pre" || program == "mc cert" || program == "dac upgrade" || program == "dacm upgrade") {
              lms = "blackboard";
            }

            // web.config setting, expected values: "moodle_EmailSubj", "blackboard_EmailSubj"
            string appSettingSubjectName = lms + "_EmailSubj"; 
           
            // web.config setting, expected values: "moodle_EmailBody_TemplateFile", "blackboard_EmailBody_TemplateFile"
            string appSettingBodyName = lms + "_EmailBody_TemplateFile"; 

            string subj = Utilities.getAppSetting(appSettingSubjectName);
            string body = Utilities.getTemplateFileContents(Utilities.getAppSetting(appSettingBodyName));
            if (0 != Utilities.sendEmail(req.Form[NewUserStagingHandler.FRMFLD_EMAIL], Utilities.subParams(subj, req), Utilities.subParams(body, req))) {
                resp.Write(PortalGuard.Utilities.BuildErrorXML("Failed to send secondary email"));
                return 1;
            }
            return PGAPI_RC_NOERROR;
        }


        private bool isInputValid(HttpRequest req) {
            // Loop over all fields that shouldn't be blank
            string[] nonblankfields = new string[] { "FirstName", "LastName", "Username", "Email", /*"EmailConfirm",*/ "Password", "PasswordConfirm" };
            foreach (string fld in nonblankfields) {
                if (isNullOrBlank(req, fld, true)) { errors.Add(new PGError(PGError.VLDERR.BLANK, fld)); }
            }

            // Ensure the terms of use have been checked/accepted
            if (!isBoxChecked(req, "acceptTOU")) { errors.Add(new PGError(PGError.VLDERR.UNCHECKED_BOX, "acceptTOU")); }

            // If any simple errors occurred, then exit before performing the more complex checks
            if (errors.Count > 0) {
                return false;
            }

            // Check username syntax
            if (!isValidUsername(req, "Username")) {
                // If it's not valid, then don't bother checking confirmation fields yet...
                errors.Add(new PGError(PGError.VLDERR.BAD_USERNAME_SYNTAX, "Username"));
                return false;
            }

            // Check email address syntax
            if (!isValidEmail(req, "Email")) {
                // If it's not valid, then don't bother checking confirmation fields yet...
                errors.Add(new PGError(PGError.VLDERR.BAD_EMAIL_SYNTAX, "Email"));
                return false;
            }

            // Now ensure the confirmation fields match
            //if (!doFieldValuesMatch(req, "Email", "EmailConfirm", false)) { errors.Add(new PGError(PGError.VLDERR.UNMATCHING_CONFIRMATION, "Email")); }
            if (!doFieldValuesMatch(req, "Password", "PasswordConfirm", false)) { errors.Add(new PGError(PGError.VLDERR.UNMATCHING_CONFIRMATION, "Password")); }

            if (errors.Count > 0) {
                return false;
            }

            return true;
        }


        private int doSQLActions(NameValueCollection form) {
            int nRet = 1;		// Error by default
            string strConn = "Server=sqlserv;Database=thedb;User ID=update_user;Password=123456;Connect Timeout=10";
            // Use parameterized query to prevent SQL injection
            string param_query = "INSERT INTO Customusers (Username, Password, Email, DspName) VALUES (@uname, @pw, @email, @dspname)";

            try {
                using (SqlConnection conn = new SqlConnection(strConn)) {
                    using (SqlCommand cmd = new SqlCommand(param_query, conn)) {
                        // Use parameterized query to prevent SQL injection
                        cmd.Parameters.Add("@uname", SqlDbType.VarChar, 100).Value = form["Username"];
                        cmd.Parameters.Add("@pw", SqlDbType.VarChar, 100).Value = form["Password"];
                        cmd.Parameters.Add("@email", SqlDbType.VarChar, 100).Value = form["Email"];
                        cmd.Parameters.Add("@dspname", SqlDbType.VarChar, 250).Value = form["FirstName"] + " " + form["LastName"];

                        conn.Open();
                        Int32 rowsAffected = cmd.ExecuteNonQuery();
                        nRet = 0;
                    }
                }
            } catch (Exception ex) {
                throw ex;
            }

            return nRet;
        }
        /*****************************************************************
         * END OF SUGGESTED CODE TO CUSTOMIZE
         *****************************************************************/


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


		// NOTE: Checkboxes are not included in POST data unless they are checked - we don't care what the actual value is
		private bool isBoxChecked(HttpRequest req, string fld) {
			if (String.IsNullOrEmpty(req.Form[fld])) {
				return false;
			}
			return true;
		}


		private bool isValidUsername(HttpRequest req, string fld) {
			string nm = req.Form[fld];
			if (String.IsNullOrEmpty(nm)) {
				return false;
			}
			// Min len: 5, Max len: 25
			Regex myregex = new Regex("^[a-zA-Z0-9][.a-zA-Z0-9_]{4,24}$");
			return myregex.IsMatch(nm);
		}
		
		
		// Directly from: http://msdn.microsoft.com/en-us/library/01escwtf.aspx
		private bool isValidEmail(HttpRequest req, string fld) {
			string email = req.Form[fld];
			if (String.IsNullOrEmpty(email)) {
				return false;
			}

			// 2016-12-09 - Now case-insensitive
			return Regex.IsMatch(email,
					  @"^(?("")(""[^""]+?""@)|(([0-9a-zA-Z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-zA-Z])@))" +
					  @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]*\.)+[a-zA-Z0-9]{2,17}))$");
		}


		private bool doFieldValuesMatch(HttpRequest req, string fld1, string fld2, bool bIgnoreCase) {
			string val1 = req.Form[fld1];
			string val2 = req.Form[fld2];

			if (null == val1 || null == val2) {
				return false;
			}

			if (0 == String.Compare(val1, val2, bIgnoreCase)) {
				return true;
			}
			return false;
		}


		private string BuildReturnXML() {
			XmlDocument xdoc = new XmlDocument();
			XmlNode node = xdoc.CreateElement("pg_return");
			xdoc.AppendChild(node);

			// Top-level error code
			node = xdoc.CreateElement("maj_error");
			if (errors.Count > 0) {
				node.InnerText = PGAPI_RC_SELFREG_FAILURE;
				xdoc.FirstChild.AppendChild(node);

				XmlNode errs_node = xdoc.CreateElement("errs");
				for (int i = 0; i < errors.Count; i++) {
					XmlNode errnode = xdoc.ImportNode((errors[i]).ToXML(), true);
					errs_node.AppendChild(errnode);
				}
				xdoc.FirstChild.AppendChild(errs_node);
			} else {
				node.InnerText = PGAPI_RC_SELFREG_SUCCESS;
				xdoc.FirstChild.AppendChild(node);
			}			

			return xdoc.InnerXml;
		}


		private string BuildErrorXML(Exception e) {
			XmlDocument xdoc = new XmlDocument();
			XmlNode node = xdoc.CreateElement("pg_return");
			xdoc.AppendChild(node);

			// Top-level error code
			node = xdoc.CreateElement("maj_error");
			node.InnerText = PGAPI_RC_SELFREG_FAILURE;
			xdoc.FirstChild.AppendChild(node);

			node = xdoc.CreateElement("err_desc");
			node.InnerText = e.ToString();
			xdoc.FirstChild.AppendChild(node);

			return xdoc.InnerXml;
		}


		private bool wasPGSuccessful(string retxml) {
			XmlDocument xdoc = new XmlDocument();
			xdoc.LoadXml(retxml);
			XmlNode node = xdoc.SelectSingleNode("//maj_error/text()");
			if (null != node)
				if (0 == node.InnerText.CompareTo(PGAPI_RC_SELFREG_SUCCESS))
					return true;
			return false;
		}


        // 2014-07-28 - These helper functions must be used to make the existing HttpRequest collections writeable!
        //  From: http://forums.asp.net/t/1826362.aspx?Question+about+using+reflection+to+alter+querystrings+in+HttpRequest+object+
        private void SetCookie(ref HttpRequest req, string nm, string val) {
            try {
                // 2014-07-31 - Can update the Cookie collection, but PG.NET parses the Cookie header so also update that!
                System.Reflection.PropertyInfo prop = req.Cookies.GetType().GetProperty("IsReadOnly", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                prop.SetValue(req.Cookies, false, null);

                // Remove the existing entry if present
                if (null != req.Cookies.Get(nm)) {
                    RemoveCookie(ref req, nm);
                    req.Cookies.Remove(nm);
                }

                // Update the "Cookie" header first, then add to the Cookie collection (for any .NET access)
                AddCookie(ref req, nm, val);

                HttpCookie cook = new HttpCookie(nm, val);
                req.Cookies.Add(cook);

                prop.SetValue(req.Cookies, true, null);
            } catch (Exception ex) {
                throw new Exception("Caught exception in SetCookie('" + nm + "'): " + ex.Message);
            }
        }

        // This function only updates the "Cookies" header, it does not update the .NET cookie collection!
        private void RemoveCookie(ref HttpRequest req, string nm) {
            try {
                System.Reflection.PropertyInfo prop = req.Headers.GetType().GetProperty("IsReadOnly", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                prop.SetValue(req.Headers, false, null);

                // Get the existing header (if present)
                string curval = req.Headers.Get("Cookie");
                if (!String.IsNullOrEmpty(curval)) {
                    HttpCookie cook = req.Cookies[nm];
                    if (null != cook) {
                        // Cookie exists - remove it from Cookie header
                        string fullval = nm + "=" + cook.Value;
                        curval = curval.Replace("; " + fullval, "");    // If the cookie is 2nd or later in string
                        curval = curval.Replace(fullval, "");           // If the cookie is the first or "only" cookie
                    }
                    req.Headers["Cookie"] = curval;
                }
                prop.SetValue(req.Headers, true, null);
            } catch (Exception ex) {
                throw new Exception("Caught exception in RemoveCookie('" + nm + "'): " + ex.Message);
            }
        }

        // This function only updates the "Cookies" header, it does not update the .NET cookie collection!
        private void AddCookie(ref HttpRequest req, string nm, string val) {
            try {
                System.Reflection.PropertyInfo prop = req.Headers.GetType().GetProperty("IsReadOnly", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                prop.SetValue(req.Headers, false, null);

                // Get the existing entry (if present)
                string curval = req.Headers.Get("Cookie");
                if (String.IsNullOrEmpty(curval)) {
                    req.Headers["Cookie"] = val;
                } else {
                    string nmlabel = nm + "=";
                    string oldval = (null == req.Cookies[nm] ? "" : req.Cookies[nm].Value);
                    if (String.IsNullOrEmpty(oldval)) {
                        curval += "; " + nmlabel + val;
                    } else {
                        oldval = nmlabel + oldval;
                        curval = curval.Replace(oldval, nmlabel + val);
                    }
                    req.Headers["Cookie"] = curval;
                }
                prop.SetValue(req.Headers, true, null);
            } catch (Exception ex) {
                throw new Exception("Caught exception in AddCookie('" + nm + "'): " + ex.Message);
            }
        }
        
        private void SetHeader(ref HttpRequest req, string nm, string val) {
            try {
                System.Reflection.PropertyInfo prop = req.Headers.GetType().GetProperty("IsReadOnly", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                prop.SetValue(req.Headers, false, null);

                // Remove the existing entry if requested
                if (null != req.Headers.Get(nm)) {
                    req.Headers.Remove(nm);
                }

                req.Headers.Add(nm, val);
                prop.SetValue(req.Headers, true, null);
            } catch (Exception ex) {
                throw new Exception("Caught exception in SetHeader('" + nm + "'): " + ex.Message);
            }
        }

        private void SetServerVariable(ref HttpRequest req, string nm, string val) {
            try {
                System.Reflection.PropertyInfo prop = req.ServerVariables.GetType().GetProperty("IsReadOnly", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                prop.SetValue(req.ServerVariables, false, null);

                // Remove the existing entry if requested
                if (null != req.ServerVariables.Get(nm)) {
                    req.ServerVariables.Remove(nm);
                }

                req.ServerVariables.Add(nm, val);
                prop.SetValue(req.ServerVariables, true, null);
            } catch (Exception ex) {
                throw new Exception("Caught exception in SetServerVariable('" + nm + "'): " + ex.Message);
            }
        }

        private void SetFormField(ref HttpRequest req, string nm, string val) {
            try {
                System.Reflection.PropertyInfo prop = req.Form.GetType().GetProperty("IsReadOnly", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                prop.SetValue(req.Form, false, null);

                // Remove the existing entry if requested
                if (null != req.Form.Get(nm)) {
                    req.Form.Remove(nm);
                }

                // Do NOT need to recalculate ContentLength since the PG.NET shim enumerates the collection items directly. It does not rely on ContentLength (as long as it's > 0!!)
                req.Form.Add(nm, val);                
                prop.SetValue(req.Form, true, null);
            } catch (Exception ex) {
                throw new Exception("Caught exception in SetFormField('" + nm + "'): " + ex.Message);
            }
        }

        private void SetQueryString(ref HttpRequest req, string nm, string val) {
            try {
                System.Reflection.PropertyInfo prop = req.QueryString.GetType().GetProperty("IsReadOnly", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                prop.SetValue(req.QueryString, false, null);

                // Remove the existing entry if requested
                if (null != req.QueryString.Get(nm)) {
                    req.QueryString.Remove(nm);
                }

                req.QueryString.Add(nm, val);
                prop.SetValue(req.QueryString, true, null);
            } catch (Exception ex) {
                throw new Exception("Caught exception in SetQueryString('" + nm + "'): " + ex.Message);
            }
        }

	}
	

	public class PGError {
		public enum VLDERR {
			BLANK = 1,
			UNMATCHING_CONFIRMATION = 2,
			BAD_USERNAME_SYNTAX = 3,
			BAD_EMAIL_SYNTAX = 4,
			UNCHECKED_BOX = 5,
		    BAD_CAPTCHA = 1322,
            UNUSABLE_USERNAME_AAD = 1305,
            UNUSABLE_USERNAME = 1304,
            UNUSABLE_EMAIL = 1305,
            BAD_PASSWORD = 1301,
            INTERNAL_ERROR = 1120
		}

		private string _fld;
		private VLDERR _code;

		public PGError(VLDERR code, string fld) {
			_code = code;
			_fld = fld;
		}

		public int Code {
			get { return (int)_code; }
			set { _code = (VLDERR)value; }
		}

		public string Field {
			get { return _fld; }
			set { _fld = value; }
		}


		public XmlNode ToXML() {
			XmlDocument xdoc = new XmlDocument();
			XmlNode node = xdoc.CreateElement("err");
			xdoc.AppendChild(node);

			node = xdoc.CreateElement("code");
			node.InnerText = Code.ToString();
			xdoc.FirstChild.AppendChild(node);
			
			node = xdoc.CreateElement("fld");
			node.InnerText = Field;
			xdoc.FirstChild.AppendChild(node);

			return xdoc.FirstChild;			
		}
	}
}
