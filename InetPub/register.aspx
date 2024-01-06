<%@ Page Language="C#" Inherits="System.Web.UI.Page" %>
<%@ Assembly Name="PG.NET, Version=1.2.0.0, Culture=neutral, PublicKeyToken=d2cafef75499a122" %>
<!DOCTYPE html>

<!-- #Include virtual="/PG_Custom_dotNET_Text.inc" -->

<script runat="server">
	public string m_Lang = "", m_LangDotted = "", m_AllLangs = "";
	private string m_pubkey = "";
	private string m_theme = "";
	private string m_error = "";
	private string m_csrftoken = "";
	private string m_jsonfields = "{}";
	private int m_ver = 0;

	void Page_Load(object sender, EventArgs e) {
		try {
			Pistolstar.Security.PGBaseAuth.getCAPTCHAInfo(HttpContext.Current, 300 /*PG_REQ_GET_CAPTCHA_INFO*/, ref m_ver, ref m_pubkey, ref m_theme, ref m_error);
		} catch (Exception ex) {
			m_error = ex.Message;
		}
		// 2015-02-04
		m_csrftoken = HttpUtility.HtmlEncode(Pistolstar.Security.PGHardening.getCSRFToken());

		// 2021-10-21 - Pacific College TA
		if (PortalGuard.Utilities.isGET(Request)) {
			if (!String.IsNullOrEmpty(Request.QueryString["token"])) {
				Dictionary<string, string> prereg = new Dictionary<string, string>();
				if (0 == PortalGuard.Utilities.getRegistration(Request.QueryString["token"], ref prereg, ref m_error)) {
					m_jsonfields = "{";
					foreach (string key in prereg.Keys) {
						m_jsonfields += String.Format("{0}'{1}':'{2}'", (m_jsonfields.Length > 1 ? "," : ""), key, prereg[key]);
					}
					m_jsonfields += "}";
				} else {
					Response.Redirect("/");	// Do NOT allow self-registration if we can't find the token!
				}
			} else {
				Response.Redirect("/");		// Do NOT allow self-registration - Redirect!
			}
		}

		// Multi-language support
		if (null != Application["PGLangs"]) {
			Pistolstar.Config.PGConfig_Languages langs = (Pistolstar.Config.PGConfig_Languages)Application["PGLangs"];
			m_Lang = langs.getLanguage(Request);
			if (m_Lang.Length > 0)
				m_LangDotted = "." + m_Lang;
			m_AllLangs = langs.getAllLanguages();
		}

		// 2013-04-09 - Prevent browser caching
		Response.Cache.SetNoStore();
		Response.Cache.SetCacheability(HttpCacheability.NoCache);
		Response.Cache.SetExpires(DateTime.Now);
	}


</script>

<html lang="en">
<head>
<meta charset="utf-8">
<meta http-equiv="X-UA-Compatible" content="IE=edge">
<meta name="viewport" content="width=device-width, initial-scale=1">

<title><%=PG_TITLE_REGISTER%></title>
    
<!-- #Include virtual="/PG_Common_CSS.inc" -->
<!-- #Include virtual="/PG_Common_JS.inc" -->
<script src="/_layouts/images/pg/js/pg_selfreg.js?r=<%=PG_RSRC_TIME%>" type="text/javascript"></script>

<!-- #Include virtual="/PG_Custom_head.inc" -->

<script type="text/javascript">
<!--
// Default "displayed" field names on the login form 
var FLD_DSP_USER 	= DEF_FLD_USERNAME;
var FLD_DSP_PASS 	= DEF_FLD_PASSWORD;
var FLD_NEWPW 		= DEF_FLD_NEWPW;
// Default "submitted" field names on the login form (not necessarily the same)
var FLD_SUBMIT_USER = DEF_FLD_USERNAME;
var FLD_SUBMIT_PASS = DEF_FLD_PASSWORD;

var SEL_LANG = "<%=m_Lang%>";
var arrLangsAvailable = [<%=m_AllLangs%>];
	
function setFieldLabels() {
	setElemContent(getSelfRegTitle(), ["lblRegisterTitle"]);
	setElemContent(getSelfRegInstr(), ["infoRegister"]);
	setElemContent(getSelfRegFirstNameLabel(), ["lblFirstName"]);
	setElemContent(getSelfRegLastNameLabel(), ["lblLastName"]);
	setElemContent(getSelfRegUsernameLabel(), ["lblUsername"]);
	setElemContent(getSelfRegEmailLabel(), ["lblEmail"]);
	setElemContent("Phone Number", ["lblPhone"]);
	//setElemContent(getSelfRegConfEmailLabel(), ["lblConfEmail"]);
	setElemContent(getSelfRegEmailOptInLabel(), ["lblEmailOptIn"]);
	setElemContent(getSelfRegPWLabel(), ["lblPW"]);
	setElemContent(getSelfRegConfPWLabel(), ["lblConfPW"]);
	setElemContent(getSelfRegTOULabel(), ["lblTOU"]);
	setElemContent(getSelfRegTOUText(), ["TOU"]);
	setElemContent(getSelfRegAcceptTOULabel(), ["lblAcceptTOU"]);
	setElemContent(getSelfRegCAPTCHALabel(), ["lblCAPTCHA"]);
	setElemContent(getBtnContinue(), ["btnRegContinue"]);
	setElemContent(getBtnCancel(), ["btnRegCancel"]);
	setElemContent(getLanguageLabel(), ["lblLangSel"]);
	setInputValue(getChangeLanguageButtonText(), ["btnChangeLang"]);
	createLangSelectors();
}

function handleEnterKey(e) {
	e = e || window.event;
	var key = e.charCode ? e.charCode : (e.keyCode ? e.keyCode : 0);
	if (key == 13) {
		submitSelfReg();
		return false;
	} else
		return true;
}
document.onkeypress = handleEnterKey;

function getElemInsensitive(frm, key) {
	for (var i = 0; i < frm.elements.length; i++) {
		if (key.toLowerCase() == frm.elements[i].name.toLowerCase())
			return frm.elements[i];
	}
	return null;
}

function window_onload() {
	$("input[type='text'], input[type='password'], select, textarea").addClass(g_defInputClass);
	
	frmMainLogon = frmMainDisplay = document.forms["RegisterForm"];
	FLD_DSP_USER = FLD_SUBMIT_USER = "Username";
	FLD_DSP_PASS = FLD_SUBMIT_PASS = "Password";

	frmMainLogon.elements["FirstName"].focus();
	
	g_bShowAllPWRules = true;
	setAuthType(PG_AUTHTYPE_SELFREG);
	strURL = "/_layouts/PG/register.ashx";
	
	setFieldLabels();
	
	var tmp = getQSVar("ReturnUrl");
	if (0 == tmp.length) {
		tmp = "/";	// 2020-01-15 - Ensures self-registration confirmation works if ReturnUrl is missing from QS
	}
	addHiddenField(frmMainLogon, "ReturnUrl", tmp);

	var allfields = <%=m_jsonfields%>;
	if (undefined !== allfields.randtoken) {
		addHiddenField(frmMainLogon, "randtoken", allfields.randtoken);
		Object.keys(allfields).forEach(function (key) {
			var el = getElemInsensitive(frmMainLogon, key);
			if (null != el) {
				el.value = allfields[key];
				el.disabled = true;
			}
		});
		frmMainLogon.elements[FLD_DSP_USER].focus();
	}

	var err = "<%=m_error%>";
	var captcha_pubkey = "<%=m_pubkey%>";
	var captcha_theme = "<%=m_theme%>";
	var captcha_version = <%=m_ver%>;
	if (captcha_pubkey.length > 0 && captcha_theme.length > 0) {
		if (null == g_CAPTCHA) {
			g_CAPTCHA = new PG_CAPTCHA();
			if (2 == captcha_version) {
				$.getScript("https://www.google.com/recaptcha/api.js?onload=cbRECAPTCHAv2OnLoad&render=explicit");
			}
		}
		
		g_CAPTCHA.key = captcha_pubkey;
		g_CAPTCHA.theme = captcha_theme;
		g_CAPTCHA.version = captcha_version;
		showRecaptcha(true, false);
	}
}
//-->
</script>
</head>

<body onload="return window_onload()">
	<div class="container hcenter">
		<div class="row">
			<div class="displaybox col-md-8 col-md-offset-2 shadow register">
				<div class="row dlghdr">
					<div class="col-md-12">
						<div class="text-center">
							<h1 id="lblRegisterTitle"></h1>
						</div>
					</div>
				</div>
				
				<form id="RegisterForm" action="" name="RegisterForm" method="post" autocomplete="off" class="form-horizontal">
					<div class="paddedContainer">
						<span id="LangSel" class="clsLangSelector" style="display:none">
							<label id="lblLangSel" for="listLangSel">Language:</label>
							<select name="listLangSel" id="listLangSel"></select><input type="button" id="btnChangeLang" onclick="changeLang(document.getElementById('listLangSel'))" value="Change">
						</span>

						<div id="infoRegister" class="popupInstructions"></div>
						<div id="ErrMsgRegister" role="status"></div>

						<div id="fldsRegister">
							<div class="form-group">
								<div class="col-md-6">
									<label for="FirstName" id="lblFirstName"></label>
									<input type="text" id="FirstName" name="FirstName" maxlength="256" />
								</div>
								<div class="col-md-6">
									<label for="LastName" id="lblLastName"></label>
									<input type="text" id="LastName" name="LastName" maxlength="256" />
								</div>
							</div>
							<div class="form-group">
								<div class="col-md-6">
									<label for="Email" id="lblEmail"></label>
									<input type="text" id="Email" name="Email" maxlength="256" />
								</div>
								<div class="col-md-6">
									<label for="Phone" id="lblPhone"></label>
									<input type="text" id="Phone" name="Phone" maxlength="256" />
								</div>
							</div>
							<div class="form-group">
								<div class="col-md-6">
									<label for="Username" id="lblUsername"></label>
									<input type="text" id="Username" name="Username" maxlength="256" />
								</div>
								<div class="col-md-6">
									<label for="Campus" id="lblCampus">Campus</label>
									<input type="text" id="Campus" name="Campus" maxlength="256" />
								</div>
							</div>
							<div class="form-group">
								<div class="col-md-6">
									<label for="Password" id="lblPW"></label>
									<input type="password" id="Password" name="Password" maxlength="256" />
								</div>
								<div class="col-md-6">
									<label for="PasswordConfirm" id="lblConfPW"></label>
									<input type="password" id="PasswordConfirm" name="PasswordConfirm" maxlength="256" />
								</div>
							</div>
							<div class="form-group" style="display:none">
								<div class="col-md-12">
									<input type="checkbox" id="optin" name="optin" value="1" />
									<label id="lblEmailOptIn" for="optin" class="lblCheck">Opt-in for email notifications?</label>
								</div>
							</div>
							<div id="fldTOU" class="form-group">
								<div class="col-md-12">
									<label for="TOU" id="lblTOU"></label>
									<textarea rows="6" id="TOU" readonly></textarea>
									<span id="fldAcceptTOU">
										<input type="checkbox" id="acceptTOU" name="acceptTOU" value="1" onclick="document.getElementById('fldAcceptTOU').className = ''" />
										<label id="lblAcceptTOU" for="acceptTOU" class="lblCheck"></label>
									</span>
								</div>
							</div>
							<div id="fldsCaptchaRegister" style="display:none" class="form-group">
								<div class="col-md-12">
									<label class="lblSpan" id="lblCAPTCHA" for="g-recaptcha-response"></label>
									<div id="divCaptchaRegister"></div>
								</div>
							</div>							
							<div class="col-md-12">
								<div class="row">
									<div class="row containerPGButton">
										<div class="col-md-6">
											<button class="PGButton btn" id="btnRegContinue" onclick="submitSelfReg(); return false;">Continue</button>
										</div>
										<div class="col-md-6">
											<button class="PGButton btn" id="btnRegCancel" onclick="cancelSelfReg(); return false;">Cancel</button>
										</div>
									</div>
								</div>
							</div>
						</div>
					</div>
						
					<input type="hidden" id="PGToken" name="PGToken" value="<%=m_csrftoken%>">
				</form>
			</div>
		</div>
	</div>

	<div id="preload">
		<!-- Unused elements that need to be hidden -->
		<span id="pwmeter1"></span>
		<span id="pwdLockString1"></span>
		<span id="pwmeter2"></span>
		<span id="spanBtnContinueDis"></span>
		<span id="spanContinueBtn"></span>
		<form id="OTPEntryForm" action="" name="OTPEntryForm" method="post" autocomplete="off">
			<input id="OTPEntryType" type="hidden" name="OTPEntryType" value="" />
		</form>
	</div>
</body>
</html> 