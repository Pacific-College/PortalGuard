// Constants
var VLDERR_BLANK					= 1;
var VLDERR_UNMATCHING_CONFIRMATION	= 2;
var VLDERR_BAD_USERNAME_SYNTAX 		= 3;
var VLDERR_BAD_EMAIL_SYNTAX			= 4;
var VLDERR_UNCHECKED_BOX			= 5;
var VLDERR_INTERNAL_ERROR			= 1120;
var VLDERR_UNUSABLE_USERNAME_AAD	= 1305;

// Globals
// Page the user should go to after logging in
var DEST_URL = "/default.aspx";
var REG_URL = "/_layouts/PG/register.aspx";


// Mapping field names to their more descriptive labels for display purposes - add any new fields with their descriptions here
var fldLabelMap = {};
fldLabelMap["FirstName"] = "First Name";
fldLabelMap["LastName"] = "Last Name";
fldLabelMap["Username"] = "Username";
fldLabelMap["Email"] = "Email Address";
fldLabelMap["EmailConfirm"] = "Confirm Email";
fldLabelMap["Password"] = "Password";
fldLabelMap["PasswordConfirm"] = "Confirm Password";
fldLabelMap["acceptTOU"] = "Terms of Use";


// Static field labels and instructions - edit as needed
function getSelfRegTitle() { return "User Self-Registration"; }
function getSelfRegConfTitle() { return "Account Confirmation"; }
function getSelfRegInstr() { return "Please complete the form below to create a user account for this website. All fields are required."; }
function getSelfRegFirstNameLabel() { return "First Name"; }
function getSelfRegLastNameLabel() { return "Last Name"; }
function getSelfRegUsernameLabel() { return "Username"; }
function getSelfRegEmailLabel() { return "Email Address"; }
function getSelfRegConfEmailLabel() { return "Confirm Email"; }
function getSelfRegEmailOptInLabel() { return " Would you like to receive marketing emails?"; }
function getSelfRegPWLabel() { return "New Password"; }
function getSelfRegConfPWLabel() { return "Confirm Password"; }
function getSelfRegTOULabel() { return "Please accept the terms of use for this website:"; }
function getSelfRegTOUText() { return "1. Terms: By accessing this web site, you are agreeing to be bound by these web site Terms and Conditions of Use, all applicable laws and regulations, and agree that you are responsible for compliance with any applicable local laws. If you do not agree with any of these terms, you are prohibited from using or accessing this site. The materials contained in this web site are protected by applicable copyright and trade mark law. 2. User License: Permission is granted to temporarily download one copy of the materials (information or software) on PortalGuard's web site for personal, non-commercial transitory viewing only. This is the grant of a license, not a transfer of title, and under this license you may not:modify or copy the materials;use the materials for any commercial purpose, or for any public display (commercial or non-commercial);attempt to decompile or reverse engineer any software contained on PortalGuard's web site;remove any copyright or other proprietary notations from the materials; or transfer the materials to another person or ''mirror'' the materials on any other server.This license shall automatically terminate if you violate any of these restrictions and may be terminated by PortalGuard at any time. Upon terminating your viewing of these materials or upon the termination of this license, you must destroy any downloaded materials in your possession whether in electronic or printed format."; }
function getSelfRegAcceptTOULabel() { return " I accept these terms of use"; }
function getSelfRegCAPTCHALabel() { return "You're not a robot, right?"; }

// Run-time messages displayed to end user - edit as needed
function getSelfRegSuccessMsg(root) {
	var emailroot = getXMLChildElement(root, "emailconf");
	if (null != emailroot) {
		var email = getXMLAttrStr(emailroot, "email");
		return showSuccess("Confirmation Email Sent", "Please follow the instructions in the email sent to <span class='boldred italic'>" + email + "</span> to complete the registration.");
	} else {
		return showSuccess("Registration Successful", "Please follow the instructions in the email that will be sent after account creation (up to 1 hour).");
	}
}
function getSelfRegConfSuccessMsg() { return showSuccess("Registration Successful", "Your account has been successfully confirmed and created.<br /><br /><a href='javascript:doLogin()'>Please login with your new account</a>"); }
function getSelfRegExceptionMsg(root) { return showError("Caught Exception", getXMLElementStr(root, "err_desc")); }
function getSelfRegErrorListHdr() { return "<div class='errordiv'>Please correct the following errors:<ul>"; }
function getSelfRegErrorListBlankMsg(fldname) { return "<li>" + fldLabelMap[fldname] + " cannot be blank</li>"; }
function getSelfRegErrorListUnmatchedMsg(fldname) { return "<li>" + fldLabelMap[fldname] + " does not match its confirmation field</li>"; }
function getSelfRegErrListUsernameMsg(fldname) { return "<li>Username must start with a letter or number and be between 5 to 25 letters, numbers, periods or '_' characters</li>"; }
function getSelfRegErrorListEmailMsg(fldname) { return "<li>Email address must be of the format: user@domain.com</li>"; }
function getSelfRegErrorListAcceptTOUMsg(fldname) { return "<li>Please accept the Terms of Use to continue registration</li>"; }
function getSelfRegErrorListUnexpectedMsg(code, fldname) { return "<li>Unexpected error code " + code + " for field '" + fldname + "'</li>"; }
function getSelfRegBadUserMsg() { return showError("Duplicate Username", "This username has already been enrolled - please enter a different one or try logging in"); }
function getSelfRegErrorAzureADConflict() { return showError("Duplicate Username", "This username already exists in Microsoft/Azure AD - please enter a different one to continue"); }
function getSelfRegDuplicateEmailMsg() { return showError("Duplicate Email Address", "This email address has already been enrolled - please enter a different one to continue"); }
function getSelfRegEmailErrorMsg() { return showError("", "An unexpected email failure occurred - please contact the administrator"); }
function getSelfRegConfIDNotFound() { 
	var ret = "You may have waited too long to visit this URL - it is only valid for a short period of time.";
	ret += "<br /><br /><a href='" + REG_URL + "'>Please try registering again</a>";
	return showError("Account Information Not Found", ret);
}
function getSelfRegDocNotSavedMsg() { return showError("Account Not Created", "Your account could not be created" + msg_CONTACT_HD); }
function getSelfRegPWContainsNameMsg() { return showError("", "Your password cannot contain your first or last name. Please address this and resubmit the form."); }

function cancelSelfReg() {
	// Determines where the user goes when canceling registration - can use the browser's history or use an explicit URL
	history.back();
	//window.location = "http://your.url.here";
}




// ***********************************************************
// NO CODE BELOW THIS POINT SHOULD NEED TO BE MODIFIED
// ***********************************************************
function submitSelfReg() {
	var frm = document.getElementById("RegisterForm");
	var thediv = document.getElementById("ErrMsgRegister");
	// 2017-02-02 - Prevent names in the new password
	try {
		var fname = document.getElementById("FirstName").value;
		var lname = document.getElementById("LastName").value;
		var newpass = document.getElementById("Password").value;
		var fpatt = new RegExp(fname, "i");
		var lpatt = new RegExp(lname, "i");		
		if (newpass.length > 0 && fname.length > 0 && lname.length > 0 && (fpatt.test(newpass) || lpatt.test(newpass))) {
			setElemContent(getSelfRegPWContainsNameMsg(), ["ErrMsgRegister"]);
			frm["Password"].focus();
			frm["Password"].select();
			frm["Password"].className = g_defInputClass + " errorfield";
			return;
		}
	} catch (e) {
		console.log(formatException("submitSelfReg(): Unable to test if password contains names, exception: ", e));
	}
	
	setAuthType(PG_AUTHTYPE_SELFREG);
	doWSPAuth(frm, thediv);
}


function submitSelfRegEmailConf() {
	var frm = document.getElementById("RegConfForm");
	var thediv = document.getElementById("ErrMsgRegConf");
	setAuthType(PG_AUTHTYPE_SELFREG_EMAILCONF);
	doWSPAuth(frm, thediv);
}


function doLogin() {
	var login_url = "/_layouts/PG/login.aspx?ReturnUrl=" + encodeURIComponent(DEST_URL);
	// Populate the login name automatically
	if (null != frmMainLogon.elements["Username"]) {
		login_url += "&pgautofulluser=";
		if (frmMainLogon.elements["Username"].value.length > 0)
			login_url += encodeURIComponent(frmMainLogon.elements["Username"].value);
		else
			login_url += encodeURIComponent(frmMainLogon.elements["Email"].value);
	}
	
	window.location = login_url;
}


function handleSelfReg(maj_err, root, frm) {
	var msg = "", fld = "", msg2 = "";	
	var	msg_hdr = "", msg_mid = "", msg_ftr = "";	// For displaying strings comprised of multiple minor errors
	
	try {
		if (PGAPI_RC_SELFREG_EMAILCONF_SUCCESS == maj_err) {
			msg = getSelfRegConfSuccessMsg();
			// Create form fields for username and email
			var fdata = getXMLChildElement(root, "formdata");
			if (null != fdata) {
				var uname = getXMLAttrStr(fdata, "username");
				var email = getXMLAttrStr(fdata, "email");
				if (uname.length > 0)
					addFormInputField(frm, "Username", uname, true);
				if (email.length > 0)
					addFormInputField(frm, "Email", email, true);
				var returl = getXMLAttrStr(fdata, "ReturnUrl");
				if (returl.length > 0)
					addFormInputField(frm, "ReturnUrl", returl, true);
			}
		} else if (PGAPI_RC_SELFREG_SUCCESS == maj_err) {
			// Display success!
			setElemVisibility(["fldsRegister", "infoRegister", "lblRegisterTitle"], false);
			msg = getSelfRegSuccessMsg(root);
		} else {
			// Handle any nasty exceptions separately
			if (null != getXMLChildElement(root, "err_desc")) {
				return getSelfRegExceptionMsg(root);
			}
			
			if (null != getXMLChildElement(root, "errs")) {
				var errors = getXMLChildElement(root, "errs").childNodes;
				for (var i = 0; i < errors.length; i++) {
					var bChangeBGColor = true;
					if (0 == i) {
						msg = getSelfRegErrorListHdr();
					}
					
					var fldname = getXMLElementStr(errors[i], "fld");
					var code = getXMLElementNum(errors[i], "code");
					switch(code) {
						case VLDERR_BLANK:
							msg += getSelfRegErrorListBlankMsg(fldname);
							break;
						case VLDERR_UNMATCHING_CONFIRMATION:
							msg += getSelfRegErrorListUnmatchedMsg(fldname);
							break;
						case VLDERR_BAD_USERNAME_SYNTAX:
							msg += getSelfRegErrListUsernameMsg(fldname);
							break;
						case VLDERR_BAD_EMAIL_SYNTAX:
							msg += getSelfRegErrorListEmailMsg(fldname);
							break;
						case VLDERR_UNCHECKED_BOX:
							if ("acceptTOU" == fldname) {
								msg += getSelfRegErrorListAcceptTOUMsg(fldname);
								bChangeBGColor = false;
								document.getElementById("fldAcceptTOU").className = g_defInputClass + " errorfield";
								break;
							}
							break;
						case VLDERR_UNUSABLE_USERNAME_AAD:
							fld = resolveField(DEF_FLD_USERNAME);
							msg = getSelfRegErrorAzureADConflict();
							break;
						case VLDERR_INTERNAL_ERROR:
							msg = getInternalErrorMsg();
							break;
						default:
							msg += getSelfRegErrorListUnexpectedMsg(code, fldname);
					}
					
					if (bChangeBGColor && fldname.length > 0) {
						frm[fldname].className = g_defInputClass + " errorfield";
					}
					
					// Put the focus in the first field that's bad
					if (0 == i && fldname.length > 0) {
						frm[fldname].focus();
						frm[fldname].select();
					}
				}
			}
			
			// These are lower-level errors from PG - any message from previous errors will be overwritten
			var bShowSetPW = false;
			var bGetAllPWRules = false;
			var root_min = getXMLChildElement(root, "min_errors");
			var min_errs = getXMLAttrNum(root_min, "count");
			for (var i = 0; i < min_errs; i++) {
				var min_err = getXMLElementNum(root_min, "min_error", i);
				if (DEBUG)
					alert("Min_error[" + i + "]: " + min_err);

				switch(min_err) {
					case PGAPI_RC_NO_USERNAME_SUPPLIED:
						fld = resolveField(DEF_FLD_USERNAME);
						msg = getBlankUserMsg();
						break;

					case PGAPI_RC_UNUSABLE_NEWUSER:
						fld = resolveField(DEF_FLD_USERNAME);
						msg = getSelfRegBadUserMsg();
						break;

					case PGAPI_RC_NO_EMAIL_SUPPLIED:
						fld = "Email";
						msg = "Please enter your email address";
						break;

					case PGAPI_RC_UNUSABLE_EMAIL:
						fld = "Email";
						msg = getSelfRegDuplicateEmailMsg();
						break;

					case PGAPI_RC_NO_CAPTCHA:
						msg = getMissingCAPTCHAMsg();
						try {
							g_bFocusCAPTCHA = true;
						} catch (e) {}
						break;

					case PGAPI_RC_BAD_CAPTCHA:
						try {
							if (null != g_CAPTCHA && 2 == g_CAPTCHA.version) {
								msg = getBadCAPTCHAMsg(2);
							} else {
								msg = getBadCAPTCHAMsg(1);
								Recaptcha.reload();
								g_bFocusCAPTCHA = true;
							}
						} catch (e) {}
						break;

					case PGAPI_RC_PW_TOO_SHORT:
					case PGAPI_RC_PW_TOO_LONG:
					case PGAPI_RC_PW_INSUFF_LCASE:
					case PGAPI_RC_PW_INSUFF_UCASE:
					case PGAPI_RC_PW_INSUFF_NUMERIC:
					case PGAPI_RC_PW_INSUFF_SPECIAL:
					case PGAPI_RC_PW_AD_COMPLEXITY:
					case PGAPI_RC_PW_DICTIONARY_HIT:
					case PGAPI_RC_PW_INSUFF_SCORE:
					case PGAPI_RC_PW_INSUFF_DIFFCHARS:
						fld = "Password";
						
						// 2011-10-31 - Is rule grouping enabled?
						var root_grp = getXMLChildElement(getXMLChildElement(root, "pwquality"), "pwgroup");
						var grp_floor = getXMLAttrNum(root_grp, "rule");
						if (DEBUG)
							alert("PW group floor count: " + grp_floor);

						msg_hdr = getPWComplexityHdr(grp_floor);
						msg_mid = getPWComplexityMid(grp_floor);
						msg_ftr = getPWComplexityServerSide(root) + getPWComplexityFtr(grp_floor);	// 2019-02-18 - Server side rules display before footer
						if (g_bShowAllPWRules) {
							bGetAllPWRules = true;
						} else {
							var bGrouped = new wrapbool();
							var tmp = getSinglePWRuleMsg(getXMLChildElement(root, "pwquality"), min_err, null, bGrouped);
							if (bGrouped.value)
								msg2 = msg2 + tmp;
							else
								msg  = msg + tmp;
						}
						break;
						
					case PGAPI_RC_AUTH_SERVER_UNAVILABLE:
						msg = getServerDownMsg();
						break;

					case PGAPI_RC_EMAIL_FAILURE:
						msg = getSelfRegEmailErrorMsg();
						break;
						
					case PGAPI_RC_UNSUPPORTED_FEATURE:
						msg = getUnsupportedMsg();
						break;
					
					case PGAPI_RC_CONFIG_ERROR:
						msg = getConfigErrorMsg();
						break;
						
					case PGAPI_RC_BAD_REQUEST_FORMAT:
						msg = getSelfRegConfIDNotFound();
						break;
					
					case PGAPI_RC_INTERNAL_ERROR:
						msg = getInternalErrorMsg();
						break;					
						
					case PGAPI_RC_DOCUMENT_NOT_SAVED:
						msg = getSelfRegDocNotSavedMsg();
						break;
										
					default:
						msg = getGenErrorMsg(min_err);
				}
			}
			
			if (fld.length > 0) {
				frm[fld].focus();
				frm[fld].select();
				frm[fld].className = g_defInputClass + " errorfield";
			}
			
			if (bGetAllPWRules) {
				var root_rule = getXMLChildElement(root, "pwquality");
				// Need to get child element and determine if the rule was met or not
				var kids = root_rule.childNodes;
				for (i = 0; i < kids.length; i++) {
					var	bGrouped = new wrapbool();
					var tmp = getSinglePWRuleMsg(root_rule, 0, kids[i].nodeName, bGrouped);
					if (bGrouped.value)
						msg2 = msg2 + tmp;
					else
						msg = msg + tmp;
				}
			}
			
			if (msg_hdr.length > 0 || msg_ftr.length > 0)
				msg = msg_hdr + msg + msg_mid + msg2 + msg_ftr;
			
			if (msg.length > 0) {
				msg += "</ul></div>";
			}
		}
	} catch (e) {
		alert(formatException("handleSelfReg(): Error parsing XML response", e));
		return "";
	}
	
	return msg;
}
