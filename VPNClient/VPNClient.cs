using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DotRas;
using System.Net;
using System.Configuration;
using System.Security.Cryptography;
using System.IO;

namespace VPNClient
{
    public partial class VPNClient : Form
    {

        public const string sEntryName = "主服务器";
        private RasHandle handle = null;


        public VPNClient()
        {
            InitializeComponent();

            //load config setting
            tServerIP.Text = GetConfigValue("serverip");
            tUsername.Text = GetConfigValue("username");

            Encoding encode = new UTF8Encoding();
            tUserkey.Text = encode.GetString(Convert.FromBase64String(GetConfigValue("password")));

            //setting UI
            bDisconnect.Enabled = false;

            //创建rasphone.pbk
            if (Directory.Exists(@"C:/ProgramData/Microsoft/Network/Connections/Pbk") == false)//如果不存在
            {
                Directory.CreateDirectory(@"C:/ProgramData/Microsoft/Network/Connections/Pbk");
            }
            if (!File.Exists(@"C:/ProgramData/Microsoft/Network/Connections/Pbk/rasphone.pbk"))
            {
                //复制文件
                File.Copy(Application.StartupPath + @"\rasphone.pbk", @"C:/ProgramData/Microsoft/Network/Connections/Pbk/rasphone.pbk", false);
            }
            this.MaximizeBox = false;
            //this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.ShowInTaskbar = false;
        }

        private void bConnect_Click(object sender, EventArgs e)
        {
            string sServerip = tServerIP.Text;
            string sUsername = tUsername.Text;
            string sPassword = tUserkey.Text;

            this.tMessage.Clear();
            createVpnEntry(sServerip);

            this.Dialer.EntryName = sEntryName;
            this.Dialer.PhoneBookPath = RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.AllUsers);

            try
            {
                this.Dialer.Credentials = new NetworkCredential(sUsername, sPassword);
                this.handle = this.Dialer.DialAsync();
                this.tMessage.AppendText("正在尝试连接...\r\n");

            }
            catch (Exception ex)
            {
                this.tMessage.AppendText(ex.ToString());
            }

        }


        private void createVpnEntry(string sVpnIp)
        {
            this.AllUsersPhoneBook.Open();

            if (this.AllUsersPhoneBook.Entries.Contains(sEntryName))
            {
                //todo
                this.AllUsersPhoneBook.Entries[sEntryName].PhoneNumber = sVpnIp;
            }
            else
            {
                try
                {
                    RasEntry entry = RasEntry.CreateVpnEntry(sEntryName, sVpnIp, RasVpnStrategy.PptpOnly, //L2tpOnly
                                                                                                          //RasDevice.GetDeviceByName("(L2TP)", RasDeviceType.Vpn));
                        RasDevice.GetDeviceByName("(PPTP)", RasDeviceType.Vpn));
                    entry.EncryptionType = RasEncryptionType.Optional;

                    this.AllUsersPhoneBook.Entries.Add(entry);
                }
                catch (Exception ex)
                {
                    tMessage.AppendText(ex.ToString());
                }
            }
        }

        /// <summary>
        /// Occurs when the dialer state changes during a connection attempt.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">An <see cref="DotRas.StateChangedEventArgs"/> containing event data.</param>
        private void Dialer_StateChanged(object sender, StateChangedEventArgs e)
        {
            //this.tMessage.AppendText(e.State.ToString() + "\r\n");
        }

        /// <summary>
        /// Occurs when the dialer has completed a dialing operation.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">An <see cref="DotRas.DialCompletedEventArgs"/> containing event data.</param>
        private void Dialer_DialCompleted(object sender, DialCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                this.tMessage.AppendText("连接取消!\r\n");
            }
            else if (e.TimedOut)
            {
                this.tMessage.AppendText("连接超时!\r\n");
            }
            else if (e.Error != null)
            {
                this.tMessage.AppendText("请检查网络连接或者账户是否异常.\r\n");
            }
            else if (e.Connected)
            {
                this.tMessage.AppendText("连接成功!\r\n");
                bConnect.Enabled = false;
                bDisconnect.Enabled = true;
                //Account state changed.
            }

            if (!e.Connected)
            {
                // The connection was not connected, disable the disconnect button.
                this.tMessage.AppendText("连接失败！\r\n");

            }
        }

        private void bDisconnect_Click(object sender, EventArgs e)
        {
            if (this.Dialer.IsBusy)
            {
                this.Dialer.DialAsyncCancel();
            }
            else
            {
                RasConnection connection = RasConnection.GetActiveConnectionByHandle(this.handle);
                if (connection != null)
                {
                    connection.HangUp();
                    bConnect.Enabled = true;
                    bDisconnect.Enabled = false;
                    tMessage.AppendText("连接成功断开\r\n");
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            //save config data
            SetConfigValue("serverip", tServerIP.Text);
            SetConfigValue("username", tUsername.Text);

            Encoding encode = new UTF8Encoding();
            SetConfigValue("password", Convert.ToBase64String(encode.GetBytes(tUserkey.Text)));

        }

        private static void SetConfigValue(string key, string value)
        {
            System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[key] == null)
            {
                config.AppSettings.Settings.Add(key, value);
            }
            else
            {
                config.AppSettings.Settings[key].Value = value;
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private static string GetConfigValue(string key)
        {
            System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[key] == null)
                return "";
            else
                return config.AppSettings.Settings[key].Value;
        }

        private void Dialer_Error(object sender, System.IO.ErrorEventArgs e)
        {
            this.tMessage.AppendText("连接失败，请检查网络异常！\r\n");
        }

        private void bRenew_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://taobao.com");
        }

        private void VPNClient_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private string Info_PHP(string url, byte[] postdata)
        {
            try
            {
                WebClient wc = new WebClient();
                wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                byte[] postData = postdata;
                byte[] responseData = wc.UploadData(url, "POST", postData); // 得到返回字符流
                var resstring = Encoding.UTF8.GetString(responseData);
                wc.Dispose();
                return resstring;
            }
            catch (Exception)
            {
                MessageBox.Show("连接超时！");
                System.Environment.Exit(0);
                throw new Exception("连接超时");
            }


        }
        // Hide to system tray
        private void VPNClient_Deactivate(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                
            }
        }

    }
}
