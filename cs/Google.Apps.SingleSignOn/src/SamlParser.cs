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
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

using ICSharpCode.SharpZipLib.Zip.Compression;

namespace Google.Apps.SingleSignOn
{
    /// <summary>
    /// Provides methods for parsing and manipulating SAML compliant Xml.
    /// </summary>
    public class SamlParser
    {
        public static readonly string DateFormatter = "yyyy-MM-ddTHH:mm:ssZ";

        private SamlParser() { ;}

        // implementation borrowed from Google's Java API.
        public static XmlDocument UnpackRequest(string packedText)
        {
            // convert from Base64
            byte[] compressedBytes = Convert.FromBase64String(packedText);

            Inflater inflater = new Inflater(true);
            inflater.SetInput(compressedBytes);

            byte[] xmlMessageBytes = new byte[20000];
            int resultLength = inflater.Inflate(xmlMessageBytes);

            if (!inflater.IsFinished)
            {
                throw new Exception("didn't allocate enough space to hold decompressed data");
            }

            string result = Encoding.UTF8.GetString(xmlMessageBytes, 0, resultLength);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);

            return doc;
        }

        public static XmlDocument CreateRawResponseXml(SamlRequestArgs args, string userName)
        {
            XmlDocument doc = GetXmlDocument("Google.Apps.SingleSignOn.SamlResponse.xml");

            XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);

            ns.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");
            ns.AddNamespace("asrt", "urn:oasis:names:tc:SAML:2.0:assertion");

            XmlNode responseNode = doc.SelectSingleNode("/samlp:Response", ns);
            responseNode.SelectSingleNode("@ID", ns).Value = SamlUtility.CreateId();
            responseNode.SelectSingleNode("@IssueInstant", ns).Value = DateTime.Now.ToString(DateFormatter);

            DateTime notBefore = DateTime.Now.AddDays(-1);
            DateTime notOnOrAfter = DateTime.Now.AddDays(1);

            XmlNode assertionNode = responseNode.SelectSingleNode("asrt:Assertion", ns);
            assertionNode.SelectSingleNode("@ID", ns).Value = SamlUtility.CreateId();

            assertionNode.SelectSingleNode("asrt:Subject/asrt:NameID", ns).InnerText = userName;

            assertionNode.SelectSingleNode("asrt:Conditions/@NotBefore", ns).Value =
                notBefore.ToString(DateFormatter);

            assertionNode.SelectSingleNode("asrt:Conditions/@NotOnOrAfter", ns).Value =
                notOnOrAfter.ToString(DateFormatter);

            assertionNode.SelectSingleNode("asrt:AuthnStatement/@AuthnInstant", ns).Value =
                DateTime.Now.ToString(DateFormatter);

            return doc;
        }


        /// <summary>
        /// Create a digitally signed SAML response string that can be posted to Google Apps SSO service
        /// to log in the specified user.
        /// </summary>
        /// <param name="packedSamlRequest">The encoded SAML request, as provided directly from Google.</param>
        /// <param name="userName">The name of the user that should be included in the response.  This user
        /// name cannot be changed after the response has been created.</param>
        /// <param name="responseXml">The SAML response XML that can be posted to Google SSO.</param>
        /// <param name="actionUrl">The URL that Google expects the SAML response to be posted to.  This
        /// value is parsed from the SAML request.</param>
        public static void CreateSignedResponse(string packedSamlRequest, string userName,
            out string responseXml, out string actionUrl)
        {
            XmlDocument request = UnpackRequest(packedSamlRequest);

            SamlRequestArgs args = new SamlRequestArgs(request);

            XmlDocument response = CreateRawResponseXml(args, userName);

            XmlDocumentSigner.Sign(response);

            // TODO: validate SAML schema: http://support.microsoft.com/kb/307379
            // http://docs.oasis-open.org/security/saml/v2.0/saml-schema-protocol-2.0.xsd
            // http://docs.oasis-open.org/security/saml/v2.0/saml-schema-protocol-2.0.xsd

            responseXml = response.OuterXml;
            actionUrl = args.AssertionConsumerServiceUrl;
        }


        /// <summary>
        /// Provides a convenience method for retrieving an Xml resource embedded in the currently executing
        /// assembly.
        /// </summary>
        private static XmlDocument GetXmlDocument(string resourceName)
        {
            XmlDocument doc = new XmlDocument();
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

            using (StreamReader reader = new StreamReader(stream))
            {
                doc.Load(reader);
            }

            return doc;
        }
    }
}
