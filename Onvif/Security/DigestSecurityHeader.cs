using CoreWCF.Channels;
using System;
using System.Text;
using System.Security.Cryptography;

namespace iSpyApplication.Onvif.Security
{
    public class DigestSecurityHeader : MessageHeader
    {
        private readonly string _username;
        private readonly string _password;
        private readonly string _uri;

        public DigestSecurityHeader(string username, string password, string uri)
        {
            _username = username;
            _password = password;
            _uri = uri;
        }

        public override string Name => "Security";

        public override string Namespace => "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

        protected override void OnWriteHeaderContents(System.Xml.XmlDictionaryWriter writer, MessageVersion messageVersion)
        {
            string nonce = GenerateNonce();
            string created = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            string combinedPassword = nonce + created + _password;

            byte[] hashedPassword = SHA1.HashData(Encoding.UTF8.GetBytes(combinedPassword));
            string passwordDigest = Convert.ToBase64String(hashedPassword);

            writer.WriteStartElement("UsernameToken");
            writer.WriteStartElement("Username");
            writer.WriteString(_username);
            writer.WriteEndElement();

            writer.WriteStartElement("Password");
            writer.WriteAttributeString("Type", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest");
            writer.WriteString(passwordDigest);
            writer.WriteEndElement();

            writer.WriteStartElement("Nonce");
            writer.WriteAttributeString("EncodingType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary");
            writer.WriteString(Convert.ToBase64String(Encoding.UTF8.GetBytes(nonce)));
            writer.WriteEndElement();

            writer.WriteStartElement("Created");
            writer.WriteAttributeString("xmlns", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
            writer.WriteString(created);
            writer.WriteEndElement();

            writer.WriteEndElement(); // UsernameToken
        }

        private static string GenerateNonce()
        {
            byte[] nonceBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonceBytes);
            }
            return Convert.ToBase64String(nonceBytes);
        }
    }
}
