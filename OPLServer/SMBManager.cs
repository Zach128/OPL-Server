using SMBLibrary;
using SMBLibrary.Authentication.GSSAPI;
using SMBLibrary.Authentication.NTLM;
using SMBLibrary.Server;
using SMBLibrary.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace OPLServer
{
    internal class SMBManager
    {
        private SMBServer server;
        private IPAddress serverAddress = IPAddress.Any;
        private SMBTransportType transportType = SMBTransportType.DirectTCPTransport;
        private NTLMAuthenticationProviderBase authenticationMechanism;
        private UserCollection users = new UserCollection();
        private string AppPath = AppDomain.CurrentDomain.BaseDirectory;

        public SMBManager ()
        {
            if (!Directory.Exists(AppPath + "PS2"))
            {
                Directory.CreateDirectory(AppPath + "PS2");
            }

            users.Add("Guest", "");
            users.Add("Guest", "Guest");
            authenticationMechanism = new IndependentNTLMAuthenticationProvider(users.GetUserPassword);

            List<ShareSettings> sharesSettings = new List<ShareSettings>();
            ShareSettings itemtoshare = new ShareSettings("PS2", AppPath + "PS2", new List<string>() { "Guest" }, new List<string>() { "Guest" });
            sharesSettings.Add(itemtoshare);

            SMBShareCollection shares = new SMBShareCollection();
            foreach (ShareSettings shareSettings in sharesSettings)
            {
                FileSystemShare share = InitializeShare(shareSettings);
                shares.Add(share);
            }

            GSSProvider securityProvider = new GSSProvider(authenticationMechanism);
            server = new SMBLibrary.Server.SMBServer(shares, securityProvider);
        }

        public SMBServer Server
        {
            get { return server; }
        }

        public void AddLogHandler(EventHandler<Utilities.LogEntry> handler)
        {
            server.LogEntryAdded += handler;
        }
        public void RemoveLogHandler(EventHandler<Utilities.LogEntry> handler)
        {
            server.LogEntryAdded += handler;
        }

        public void StartServer()
        {
            Server.Start(serverAddress, transportType, true, false);
        }

        public void StopServer()
        {
            Server.Stop();
        }

        public void setServerPort(int servPort)
        {
            // Use reflection to set the ports of the client and server. Would be preferrable to change this to something less hacky.
            typeof(SMBLibrary.Client.SMB1Client).GetField("DirectTCPPort").SetValue(null, servPort);
            typeof(SMBLibrary.Client.SMB2Client).GetField("DirectTCPPort").SetValue(null, servPort);
            typeof(SMBServer).GetField("DirectTCPPort").SetValue(null, servPort);
        }

        public static FileSystemShare InitializeShare(ShareSettings shareSettings)
        {
            string shareName = shareSettings.ShareName;
            string sharePath = shareSettings.SharePath;
            List<string> readAccess = shareSettings.ReadAccess;
            List<string> writeAccess = shareSettings.WriteAccess;
            FileSystemShare share = new FileSystemShare(shareName, new NTDirectoryFileSystem(sharePath));
            share.AccessRequested += delegate (object sender, AccessRequestArgs args)
            {
                readAccess.Contains("Users");
                writeAccess.Contains("Users");
                bool hasReadAccess = Contains(readAccess, "Users") || Contains(readAccess, args.UserName);
                bool hasWriteAccess = Contains(writeAccess, "Users") || Contains(writeAccess, args.UserName);
                if (args.RequestedAccess == FileAccess.Read)
                {
                    args.Allow = hasReadAccess;
                }
                else if (args.RequestedAccess == FileAccess.Write)
                {
                    args.Allow = hasWriteAccess;
                }
                else // FileAccess.ReadWrite
                {
                    args.Allow = hasReadAccess && hasWriteAccess;
                }
            };
            return share;
        }

        public static bool Contains(List<string> list, string value)
        {
            return (IndexOf(list, value) >= 0);
        }

        public static int IndexOf(List<string> list, string value)
        {
            for (int index = 0; index < list.Count; index++)
            {
                if (string.Equals(list[index], value, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }
            return -1;
        }
    }
}
