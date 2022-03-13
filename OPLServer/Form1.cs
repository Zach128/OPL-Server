using System;
using System.Collections.Generic;
using System.Windows.Forms;

using SMBLibrary;
using SMBLibrary.Authentication.NTLM;
using SMBLibrary.Server;
using System.Net;
using SMBLibrary.Win32;
using System.IO;
using SMBLibrary.Authentication.GSSAPI;
using Utilities;

namespace OPLServer
{
    public partial class Form1 : Form
    {
        private LogWriter m_logWriter;
        private GlobalSettings settings = GlobalSettings.Instance;
        private SMBManager smbManager;
        public delegate void addLog(string a, string b, string c, string d);
        public bool isLoadingSettings = false;

        public Form1()
        {
            InitializeComponent();
            loadSettings();

            smbManager = new SMBManager();

            m_logWriter = new LogWriter();

            // If logging is enabled, register the logging method with the SMBServer.
            if (tsbEnableLog.Checked) smbManager.AddLogHandler(m_server_LogEntryAdded);

            string[] args = Environment.GetCommandLineArgs();

            foreach (string arg in args)
            {
                if (arg.ToUpper() == "/NOLOG")
                {
                    addLogList(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Information", "Commandline", "/NOLOG");
                    tsbEnableLog.Checked = false;
                }

                if (arg.ToUpper() == "/START")
                {
                    addLogList(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Information", "Commandline", "/START");
                    tsbServerState.Checked = true;
                    //tsbServerState_CheckedChanged(null, null);
                }                
            }
        }

        void loadSettings()
        {
            isLoadingSettings = true;
            if (settings.getSetting("ServerPort") != "") tstbPort.Text = settings.getSetting("ServerPort");
            if (settings.getSetting("EnableLog") == "1") { tsbEnableLog.Checked = true; } else { tsbEnableLog.Checked = false; }
            if (settings.getSetting("AutoScroll") == "1") { tsbAutoScroll.Checked = true; } else { tsbAutoScroll.Checked = false; }
            if (settings.getSetting("LogCritical") == "1") { tsbLogCritical.Checked = true; } else { tsbLogCritical.Checked = false; }
            if (settings.getSetting("LogDebug") == "1") { tsbLogDebug.Checked = true; } else { tsbLogDebug.Checked = false; }
            if (settings.getSetting("LogError") == "1") { tsbLogError.Checked = true; } else { tsbLogError.Checked = false; }
            if (settings.getSetting("LogInfo") == "1") { tsbLogInfo.Checked = true; } else { tsbLogInfo.Checked = false; }
            if (settings.getSetting("LogTrace") == "1") { tsbLogTrace.Checked = true; } else { tsbLogTrace.Checked = false; }
            if (settings.getSetting("LogVerbose") == "1") { tsbLogVerbose.Checked = true; } else { tsbLogVerbose.Checked = false; }
            if (settings.getSetting("LogWarn") == "1") { tsbLogWarn.Checked = true; } else { tsbLogWarn.Checked = false; }
            isLoadingSettings = false;
        }

        void saveSettings()
        {
            if (isLoadingSettings) return;

            settings.setSetting("ServerPort", tstbPort.Text);
            settings.setSetting("EnableLog", tsbEnableLog.Checked ? "1" : "0");
            settings.setSetting("AutoScroll", tsbAutoScroll.Checked  ? "1" : "0");
            settings.setSetting("LogCritical", tsbLogCritical.Checked  ? "1" : "0");
            settings.setSetting("LogDebug", tsbLogDebug.Checked  ? "1" : "0");
            settings.setSetting("LogError", tsbLogError.Checked  ? "1" : "0");
            settings.setSetting("LogInfo", tsbLogInfo.Checked  ? "1" : "0");
            settings.setSetting("LogTrace", tsbLogTrace.Checked  ? "1" : "0");
            settings.setSetting("LogVerbose", tsbLogVerbose.Checked  ? "1" : "0");
            settings.setSetting("LogWarn", tsbLogWarn.Checked ? "1" : "0");
        }

        void m_server_LogEntryAdded(object sender, LogEntry e)
        {
            if (e.Severity == Severity.Critical && tsbLogCritical.Checked == false) return;
            if (e.Severity == Severity.Debug && tsbLogDebug.Checked == false) return;
            if (e.Severity == Severity.Error && tsbLogError.Checked == false) return;
            if (e.Severity == Severity.Information && tsbLogInfo.Checked == false) return;
            if (e.Severity == Severity.Trace && tsbLogTrace.Checked == false) return;
            if (e.Severity == Severity.Verbose && tsbLogVerbose.Checked == false) return;
            if (e.Severity == Severity.Warning && tsbLogWarn.Checked == false) return;

            addLogList(e.Time.ToString("yyyy-MM-dd HH:mm:ss"),e.Severity.ToString(),e.Source,e.Message);
        }

        public void addLogList(string time, string seve, string source, string messg)
        {
            if (listView1.InvokeRequired)
            {
                addLog tmplog = new addLog(addLogList);
                this.Invoke(tmplog, time,seve,source,messg);
            }
            else
            {
                ListViewItem item = new ListViewItem(time);
                item.SubItems.Add(seve);
                item.SubItems.Add(source);
                item.SubItems.Add(messg);

                listView1.Items.Add(item);
                if (tsbAutoScroll.Checked) item.EnsureVisible();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Form1_Resize(sender, e);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            smbManager.StopServer();
            m_logWriter.CloseLogFile();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            listView1.Columns[0].Width = 130;
            listView1.Columns[1].Width = 100;
            listView1.Columns[2].Width = 80;
            listView1.Columns[3].Width = (this.Width-50) - listView1.Columns[0].Width - listView1.Columns[1].Width - listView1.Columns[2].Width;
        }

        private void tsbServerState_CheckedChanged(object sender, EventArgs e)
        {
            tstbPort_Leave(sender, e);

            if (tsbServerState.Checked)
            {
                try
                {
                    smbManager.StartServer();
                }
                catch (Exception ex)
                {
                    tsbServerState.Checked = false;
                    tsbServerState.Image = Properties.Resources.start;
                    tsbServerState.Text = "Server is stopped (press to start)";
                    tstbPort.Enabled = true;
                    MessageBox.Show(ex.Message, "Error");
                    return;
                }

                m_logWriter.CloseLogFile();
                tsbServerState.Image = Properties.Resources.stop;
                tsbServerState.Text = "Server is running (press to stop)";
                tstbPort.Enabled = false;
            }
            else
            {
                smbManager.StopServer();
                m_logWriter.CloseLogFile();
                tsbServerState.Image = Properties.Resources.start;
                tsbServerState.Text = "Server is stopped (press to start)";
                tstbPort.Enabled = true;
            }
            saveSettings();
        }

        private void tsbEnableLog_CheckedChanged(object sender, EventArgs e)
        {
            if (tsbEnableLog.Checked)
            {
                smbManager.AddLogHandler(m_server_LogEntryAdded);
                listView1.Enabled = true;
            }
            else
            {
                smbManager.RemoveLogHandler(m_server_LogEntryAdded);
                listView1.Enabled = false;
            }
            saveSettings();
        }

        private void tsbClearLog_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            frmAbout tmpFrm = new frmAbout();
            tmpFrm.ShowDialog(this);
        }

        private void tstbPort_Leave(object sender, EventArgs e)
        {
            int finalport;

            if (int.TryParse(tstbPort.Text, out finalport))
            {
                if (finalport > 0 && finalport < 1025)
                {
                    smbManager.setServerPort(finalport);
                    tstbPort.Text = finalport.ToString();
                    saveSettings();
                }
                else
                {
                    MessageBox.Show("The server port has to be set between 1 and 1024", "Server port range", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    tstbPort.Text = "1024";
                    tstbPort.Focus();
                }
            }
            else
            {
                MessageBox.Show("The server port has to be set between 1 and 1024", "Server port range", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                tstbPort.Text = "1024";
                tstbPort.Focus();
            }
        }

        private void tsbSettingChanged_CheckedChanged(object sender, EventArgs e)
        {
            saveSettings();
        }
    }
}
