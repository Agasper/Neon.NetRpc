using System;
using Google.Protobuf;
using Neon.Logging;
using Neon.ServerExample.Proto;
using UnityEngine;
using ILogger = Neon.Logging.ILogger;

namespace Neon.ClientExample.Net
{
    //Helper class for retrieving player's credentials
    public static class CredentialsStorage
    {
        static ILogger logger = LogManager.Default.GetLogger(nameof(CredentialsStorage));
        const string CREDENTIALS_KEY = "credentials";
        
        public static LoginCredentialsProto GetCredentials()
        {
            if (PlayerPrefs.HasKey(CREDENTIALS_KEY))
            {
                byte[] credentialsByte = Convert.FromBase64String(PlayerPrefs.GetString(CREDENTIALS_KEY));
                LoginCredentialsProto credentials = new LoginCredentialsProto();
                credentials.MergeFrom(credentialsByte);
                logger.Info($"Found credentials with id {credentials.Id}");
                return credentials;
            }
            
            logger.Info($"Credentials not found");
            return null;
        }

        public static void SetCredentials(LoginCredentialsProto credentials)
        {
            if (credentials == null) throw new ArgumentNullException(nameof(credentials));
            PlayerPrefs.SetString(CREDENTIALS_KEY, Convert.ToBase64String(credentials.ToByteArray()));
            PlayerPrefs.Save();
        }

        public static void ClearCredentials()
        {
            PlayerPrefs.DeleteKey(CREDENTIALS_KEY);
            PlayerPrefs.Save();
        }
    }
}