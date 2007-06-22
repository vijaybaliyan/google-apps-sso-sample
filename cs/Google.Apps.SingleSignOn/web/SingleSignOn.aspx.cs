// Copyright 2006 Google Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

using Google.Apps.SingleSignOn;

namespace Google.Apps.SingleSignOn.Web
{
    /// <summary>
    /// Summary description for SingleSignOn.
    /// </summary>
    public partial class SingleSignOn : System.Web.UI.Page
    {

        protected Literal LiteralAssertionUrl;

        protected string _actionUrl = string.Empty;

        protected string ActionUrl
        {
            get { return _actionUrl; }
        }

        protected void Page_Load(object sender, System.EventArgs e)
        {
            string username = Request.Form["Username"];
            string password = Request.Form["Password"];
            // validate credentials before proceeding
            if (username == null)
            {
                username = "appstester";
            }
            SetupGoogleLoginForm(username);
        }

        private void SetupGoogleLoginForm(string userName)
        {
            string samlRequest = Request.QueryString["SAMLRequest"];
            if (samlRequest == null)
            {
                samlRequest = Request.Form["SAMLRequest"];
            }
            string relayState = Request.QueryString["RelayState"];
            if (relayState == null)
            {
                relayState = Request.Form["RelayState"];
            }

            if (samlRequest != null && relayState != null)
            {
                string responseXml;
                string actionUrl;

                SamlParser.CreateSignedResponse(
                    samlRequest, userName, out responseXml, out actionUrl);

                _actionUrl = actionUrl;

                SAMLResponse.Value = responseXml;
                RelayState.Value = relayState;
            }
        }

        #region Web Form Designer generated code
        override protected void OnInit(EventArgs e)
        {
            //
            // CODEGEN: This call is required by the ASP.NET Web Form Designer.
            //
            InitializeComponent();
            base.OnInit(e);
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
        }
        #endregion
    }
}
