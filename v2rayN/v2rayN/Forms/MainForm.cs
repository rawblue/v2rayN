﻿using System;
using System.IO.Compression;
using System.Text;
using System.Windows.Forms;
using v2rayN.Handler;
using v2rayN.HttpProxyHandler;
using v2rayN.Mode;

namespace v2rayN.Forms
{
    public partial class MainForm : BaseForm
    {
        private V2rayHandler v2rayHandler;

        private PACListHandle pacListHandle;
        private V2rayUpdateHandle v2rayUpdateHandle;

        #region Window 事件

        public MainForm()
        {
            InitializeComponent();
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Text = Utils.GetVersion();

            Application.ApplicationExit += (sender, args) =>
            {
                Utils.ClearTempPath();
            };
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            ConfigHandler.LoadConfig(ref config);
            v2rayHandler = new V2rayHandler();
            v2rayHandler.ProcessEvent += v2rayHandler_ProcessEvent;
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            InitServersView();
            RefreshServers();

            LoadV2ray();

            //自动从网络同步本地时间
            if (config.autoSyncTime)
            {
                //CDateTime.SetLocalTime();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;

                HideForm();
                return;
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                HideForm();
            }
        }

        #endregion

        #region 显示服务器 listview 和 menu

        /// <summary>
        /// 刷新服务器
        /// </summary>
        private void RefreshServers()
        {
            RefreshServersView();
            RefreshServersMenu();
        }

        /// <summary>
        /// 初始化服务器列表
        /// </summary>
        private void InitServersView()
        {
            lvServers.Items.Clear();

            lvServers.GridLines = true;
            lvServers.FullRowSelect = true;
            lvServers.View = View.Details;
            lvServers.Scrollable = true;
            lvServers.MultiSelect = false;
            lvServers.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            lvServers.Columns.Add("", 30, HorizontalAlignment.Center);
            lvServers.Columns.Add("服务类型", 80, HorizontalAlignment.Left);
            lvServers.Columns.Add("别名", 100, HorizontalAlignment.Left);
            lvServers.Columns.Add("地址", 100, HorizontalAlignment.Left);
            lvServers.Columns.Add("端口", 60, HorizontalAlignment.Left);
            //lvServers.Columns.Add("用户ID(id)", 110, HorizontalAlignment.Left);
            //lvServers.Columns.Add("额外ID(alterId)", 110, HorizontalAlignment.Left);
            lvServers.Columns.Add("加密方式", 100, HorizontalAlignment.Left);
            lvServers.Columns.Add("传输协议", 60, HorizontalAlignment.Left);
            lvServers.Columns.Add("延迟", 50, HorizontalAlignment.Left);

        }

        /// <summary>
        /// 刷新服务器列表
        /// </summary>
        private void RefreshServersView()
        {
            lvServers.Items.Clear();

            for (int k = 0; k < config.vmess.Count; k++)
            {
                string def = string.Empty;
                if (config.index.Equals(k))
                {
                    def = "√";
                }

                VmessItem item = config.vmess[k];
                ListViewItem lvItem = new ListViewItem(new string[]
                {
                    def,
                    ((EConfigType)item.configType).ToString(),
                    item.remarks,
                    item.address,
                    item.port.ToString(),
                    //item.id,
                    //item.alterId.ToString(),
                    item.security,
                    item.network,
                    ""
                });
                lvServers.Items.Add(lvItem);
            }
        }

        /// <summary>
        /// 刷新托盘服务器菜单
        /// </summary>
        private void RefreshServersMenu()
        {
            menuServers.DropDownItems.Clear();

            for (int k = 0; k < config.vmess.Count; k++)
            {
                VmessItem item = config.vmess[k];
                string name = item.getSummary();

                ToolStripMenuItem ts = new ToolStripMenuItem(name);
                ts.Tag = k;
                if (config.index.Equals(k))
                {
                    ts.Checked = true;
                }
                ts.Click += new EventHandler(ts_Click);
                menuServers.DropDownItems.Add(ts);
            }
        }

        private void ts_Click(object sender, EventArgs e)
        {
            try
            {
                ToolStripItem ts = (ToolStripItem)sender;
                int index = Utils.ToInt(ts.Tag);
                SetDefaultServer(index);
            }
            catch
            {
            }
        }

        private void lvServers_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = -1;
            try
            {
                if (lvServers.SelectedIndices.Count > 0)
                {
                    index = lvServers.SelectedIndices[0];
                }
            }
            catch
            {
            }
            if (index < 0)
            {
                return;
            }
            qrCodeControl.showQRCode(index, config);
        }

        #endregion

        #region v2ray 操作

        /// <summary>
        /// 载入V2ray
        /// </summary>
        private void LoadV2ray()
        {
            if (Global.reloadV2ray)
            {
                ClearMsg();
            }
            v2rayHandler.LoadV2ray(config);
            Global.reloadV2ray = false;

            ChangeSysAgent(config.sysAgentEnabled);
        }

        /// <summary>
        /// 关闭V2ray
        /// </summary>
        private void CloseV2ray()
        {
            ConfigHandler.ToJsonFile(config);

            ChangeSysAgent(false);

            v2rayHandler.V2rayStop();
        }

        #endregion

        #region 功能按钮

        private void lvServers_DoubleClick(object sender, EventArgs e)
        {
            int index = GetLvSelectedIndex();
            if (index < 0)
            {
                return;
            }

            if (config.vmess[index].configType == (int)EConfigType.Vmess)
            {
                AddServerForm fm = new AddServerForm();
                fm.EditIndex = index;
                if (fm.ShowDialog() == DialogResult.OK)
                {
                    //刷新
                    RefreshServers();
                    LoadV2ray();
                }
            }
            else if (config.vmess[index].configType == (int)EConfigType.Shadowsocks)
            {
                AddServer3Form fm = new AddServer3Form();
                fm.EditIndex = index;
                if (fm.ShowDialog() == DialogResult.OK)
                {
                    //刷新
                    RefreshServers();
                    LoadV2ray();
                }
            }
            else
            {
                AddServer2Form fm2 = new AddServer2Form();
                fm2.EditIndex = index;
                if (fm2.ShowDialog() == DialogResult.OK)
                {
                    //刷新
                    RefreshServers();
                    LoadV2ray();
                }
            }

        }

        private void lvServers_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    menuSetDefaultServer_Click(null, null);
                    break;
                case Keys.Delete:
                    menuRemoveServer_Click(null, null);
                    break;
                case Keys.U:
                    menuMoveUp_Click(null, null);
                    break;
                case Keys.D:
                    menuMoveDown_Click(null, null);
                    break;
            }
        }

        private void menuAddVmessServer_Click(object sender, EventArgs e)
        {
            AddServerForm fm = new AddServerForm();
            fm.EditIndex = -1;
            if (fm.ShowDialog() == DialogResult.OK)
            {
                //刷新
                RefreshServers();
                LoadV2ray();
            }
        }

        private void menuRemoveServer_Click(object sender, EventArgs e)
        {
            int index = GetLvSelectedIndex();
            if (index < 0)
            {
                return;
            }
            if (UI.ShowYesNo("是否确定移除服务器?") == DialogResult.No)
            {
                return;
            }
            if (ConfigHandler.RemoveServer(ref config, index) == 0)
            {
                //刷新
                RefreshServers();
                LoadV2ray();
            }
        }

        private void menuCopyServer_Click(object sender, EventArgs e)
        {
            int index = GetLvSelectedIndex();
            if (index < 0)
            {
                return;
            }
            if (ConfigHandler.CopyServer(ref config, index) == 0)
            {
                //刷新
                RefreshServers();
            }
        }

        private void menuSetDefaultServer_Click(object sender, EventArgs e)
        {
            int index = GetLvSelectedIndex();
            if (index < 0)
            {
                return;
            }
            SetDefaultServer(index);
        }

        private void menuPingServer_Click(object sender, EventArgs e)
        {
            bgwPing.RunWorkerAsync();
        }


        private void menuExport2ClientConfig_Click(object sender, EventArgs e)
        {
            int index = GetLvSelectedIndex();
            if (index < 0)
            {
                return;
            }
            if (config.vmess[index].configType != (int)EConfigType.Vmess)
            {
                UI.Show("非Vmess服务，此功能无效");
                return;
            }

            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filter = "Config|*.json";
            fileDialog.FilterIndex = 2;
            fileDialog.RestoreDirectory = true;
            if (fileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            string fileName = fileDialog.FileName;
            if (Utils.IsNullOrEmpty(fileName))
            {
                return;
            }
            Config configCopy = Utils.DeepCopy<Config>(config);
            configCopy.index = index;
            string msg;
            if (V2rayConfigHandler.Export2ClientConfig(configCopy, fileName, out msg) != 0)
            {
                UI.Show(msg);
            }
            else
            {
                UI.Show(string.Format("客户端配置文件保存在:{0}", fileName));
            }
        }

        private void menuExport2ServerConfig_Click(object sender, EventArgs e)
        {
            int index = GetLvSelectedIndex();
            if (index < 0)
            {
                return;
            }
            if (config.vmess[index].configType != (int)EConfigType.Vmess)
            {
                UI.Show("非Vmess服务，此功能无效");
                return;
            }

            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filter = "Config|*.json";
            fileDialog.FilterIndex = 2;
            fileDialog.RestoreDirectory = true;
            if (fileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            string fileName = fileDialog.FileName;
            if (Utils.IsNullOrEmpty(fileName))
            {
                return;
            }
            Config configCopy = Utils.DeepCopy<Config>(config);
            configCopy.index = index;
            string msg;
            if (V2rayConfigHandler.Export2ServerConfig(configCopy, fileName, out msg) != 0)
            {
                UI.Show(msg);
            }
            else
            {
                UI.Show(string.Format("服务端配置文件保存在:{0}", fileName));
            }
        }

        private void menuExport2ShareUrl_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            for (int k = 0; k < config.vmess.Count; k++)
            {
                string url = ConfigHandler.GetVmessQRCode(config, k);
                if (Utils.IsNullOrEmpty(url))
                {
                    continue;
                }
                sb.Append(url);
                sb.AppendLine();
            }
            if (sb.Length > 0)
            {
                Utils.SetClipboardData(sb.ToString());
                UI.Show(string.Format("批量导出分享URL至剪贴板成功"));
            }
        }


        private void tsbOptionSetting_Click(object sender, EventArgs e)
        {
            OptionSettingForm fm = new OptionSettingForm();
            if (fm.ShowDialog() == DialogResult.OK)
            {
                //刷新
                RefreshServers();
                LoadV2ray();
            }
        }

        private void tsbReload_Click(object sender, EventArgs e)
        {
            Global.reloadV2ray = true;
            LoadV2ray();
        }

        private void tsbClose_Click(object sender, EventArgs e)
        {

            this.WindowState = FormWindowState.Minimized;
        }

        /// <summary>
        /// 设置活动服务器
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private int SetDefaultServer(int index)
        {
            if (index < 0)
            {
                UI.Show("请先选择服务器");
                return -1;
            }
            if (ConfigHandler.SetDefaultServer(ref config, index) == 0)
            {
                //刷新
                RefreshServers();
                LoadV2ray();
            }
            return 0;
        }

        /// <summary>
        /// 取得ListView选中的行
        /// </summary>
        /// <returns></returns>
        private int GetLvSelectedIndex()
        {
            int index = -1;
            try
            {
                if (lvServers.SelectedIndices.Count <= 0)
                {
                    UI.Show("请先选择服务器");
                    return index;
                }

                index = lvServers.SelectedIndices[0];
                return index;
            }
            catch
            {
                return index;
            }
        }

        private void menuAddCustomServer_Click(object sender, EventArgs e)
        {
            UI.Show("注意,自定义配置：" +
                    "\r\n完全依赖您自己的配置，不能使用所有设置功能。" +
                    "\r\n在自定义配置inbound中有socks port等于设置中的port时，系统代理才可用");

            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = false;
            fileDialog.Filter = "Config|*.json|所有文件|*.*";
            if (fileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            string fileName = fileDialog.FileName;
            if (Utils.IsNullOrEmpty(fileName))
            {
                return;
            }

            if (ConfigHandler.AddCustomServer(ref config, fileName) == 0)
            {
                //刷新
                RefreshServers();
                LoadV2ray();
                UI.Show(string.Format("成功导入自定义配置服务器"));
            }
            else
            {
                UI.Show(string.Format("导入自定义配置服务器失败"));
            }
        }

        private void menuAddShadowsocksServer_Click(object sender, EventArgs e)
        {
            HideForm();
            AddServer3Form fm = new AddServer3Form();
            fm.EditIndex = -1;
            if (fm.ShowDialog() == DialogResult.OK)
            {
                //刷新
                RefreshServers();
                LoadV2ray();
            }
            ShowForm();
        }

        private void menuAddServers_Click(object sender, EventArgs e)
        {
            string clipboardData = Utils.GetClipboardData();
            if (Utils.IsNullOrEmpty(clipboardData))
            {
                return;
            }
            int countServers = 0;
            string[] arrData = clipboardData.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            foreach (string str in arrData)
            {
                string msg;
                VmessItem vmessItem = V2rayConfigHandler.ImportFromClipboardConfig(str, out msg);
                if (vmessItem == null)
                {
                    continue;
                }
                if (vmessItem.configType == (int)EConfigType.Vmess)
                {
                    if (ConfigHandler.AddServer(ref config, vmessItem, -1) == 0)
                    {
                        countServers++;
                    }
                }
                else if (vmessItem.configType == (int)EConfigType.Shadowsocks)
                {
                    if (ConfigHandler.AddShadowsocksServer(ref config, vmessItem, -1) == 0)
                    {
                        countServers++;
                    }
                }
            }
            if (countServers > 0)
            {
                RefreshServers();
                UI.Show(string.Format("从剪贴板导入批量URL成功"));
            }
        }

        #endregion


        #region 提示信息

        /// <summary>
        /// 消息委托
        /// </summary>
        /// <param name="notify"></param>
        /// <param name="msg"></param>
        void v2rayHandler_ProcessEvent(bool notify, string msg)
        {
            try
            {
                AppendText(msg);
                if (notify)
                {
                    notifyMsg(msg);
                }
            }
            catch
            {
            }
        }

        delegate void AppendTextDelegate(string text);

        void AppendText(string text)
        {
            if (this.txtMsgBox.InvokeRequired)
            {
                Invoke(new AppendTextDelegate(AppendText), new object[] { text });
            }
            else
            {
                //this.txtMsgBox.AppendText(text);
                ShowMsg(text);
            }
        }

        /// <summary>
        /// 提示信息
        /// </summary>
        /// <param name="msg"></param>
        private void ShowMsg(string msg)
        {
            this.txtMsgBox.AppendText(msg);
            if (!msg.EndsWith("\r\n"))
            {
                this.txtMsgBox.AppendText("\r\n");
            }
        }

        /// <summary>
        /// 清除信息
        /// </summary>
        private void ClearMsg()
        {
            this.txtMsgBox.Clear();
        }

        /// <summary>
        /// 托盘信息
        /// </summary>
        /// <param name="msg"></param>
        private void notifyMsg(string msg)
        {
            notifyMain.Text = msg;
        }

        #endregion


        #region 托盘事件

        private void notifyMain_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                ShowForm();
            }
        }

        private void menuExit_Click(object sender, EventArgs e)
        {
            CloseV2ray();

            this.Visible = false;
            this.Close();
            //this.Dispose();
            //System.Environment.Exit(System.Environment.ExitCode);
            Application.Exit();
        }
        private void menuDonate_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Global.DonateUrl);
        }

        private void menuAbout_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Global.AboutUrl);
        }

        private void ShowForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            //this.notifyIcon1.Visible = false;
            this.ShowInTaskbar = true;
        }

        private void HideForm()
        {
            this.WindowState = FormWindowState.Minimized;
            this.Hide();
            this.notifyMain.Visible = true;
            this.ShowInTaskbar = false;
        }

        #endregion

        #region 后台测速

        private void bgwPing_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                for (int k = 0; k < config.vmess.Count; k++)
                {
                    if (config.vmess[k].configType == (int)EConfigType.Custom)
                    {
                        continue;
                    }
                    long time = Utils.Ping(config.vmess[k].address);
                    bgwPing.ReportProgress(k, string.Format("{0}ms", time));
                }
            }
            catch
            {
            }
        }

        private void bgwPing_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            try
            {
                int k = e.ProgressPercentage;
                string time = Convert.ToString(e.UserState);
                lvServers.Items[k].SubItems[lvServers.Items[k].SubItems.Count - 1].Text = time;

            }
            catch
            {
            }
        }

        #endregion

        #region 移动服务器

        private void menuMoveTop_Click(object sender, EventArgs e)
        {
            MoveServer(EMove.Top);
        }

        private void menuMoveUp_Click(object sender, EventArgs e)
        {
            MoveServer(EMove.Up);
        }

        private void menuMoveDown_Click(object sender, EventArgs e)
        {
            MoveServer(EMove.Down);
        }

        private void menuMoveBottom_Click(object sender, EventArgs e)
        {
            MoveServer(EMove.Bottom);
        }

        private void MoveServer(EMove eMove)
        {
            int index = GetLvSelectedIndex();
            if (index < 0)
            {
                UI.Show("请先选择服务器");
                return;
            }
            if (ConfigHandler.MoveServer(ref config, index, eMove) == 0)
            {
                //刷新
                RefreshServers();
                LoadV2ray();
            }
        }

        #endregion

        #region 系统代理相关

        private void menuCopyPACUrl_Click(object sender, EventArgs e)
        {
            Utils.SetClipboardData(HttpProxyHandle.GetPacUrl());
        }

        private void menuSysAgentEnabled_Click(object sender, EventArgs e)
        {
            bool isChecked = !config.sysAgentEnabled;
            config.sysAgentEnabled = isChecked;
            ChangeSysAgent(isChecked);
        }

        private void menuGlobal_Click(object sender, EventArgs e)
        {
            config.listenerType = 1;
            ChangePACButtonStatus(1);
        }

        private void menuPAC_Click(object sender, EventArgs e)
        {
            config.listenerType = 2;
            ChangePACButtonStatus(2);
        }

        private void menuKeep_Click(object sender, EventArgs e)
        {
            config.listenerType = 0;
            ChangePACButtonStatus(0);
        }

        private void ChangePACButtonStatus(int type)
        {
            if (HttpProxyHandle.Update(config, false))
            {
                switch (type)
                {
                    case 0:
                        menuGlobal.Checked = false;
                        menuKeep.Checked = true;
                        menuPAC.Checked = false;
                        break;
                    case 1:
                        menuGlobal.Checked = true;
                        menuKeep.Checked = false;
                        menuPAC.Checked = false;
                        break;
                    case 2:
                        menuGlobal.Checked = false;
                        menuKeep.Checked = false;
                        menuPAC.Checked = true;
                        break;
                }
            }

        }

        /// <summary>
        /// 改变系统代理
        /// </summary>
        /// <param name="isChecked"></param>
        private void ChangeSysAgent(bool isChecked)
        {
            if (isChecked)
            {
                if (HttpProxyHandle.RestartHttpAgent(config, true))
                {
                    ChangePACButtonStatus(config.listenerType);
                }
            }
            else
            {
                HttpProxyHandle.Update(config, true);
                HttpProxyHandle.CloseHttpAgent(config);
            }

            menuSysAgentEnabled.Checked =
            menuSysAgentMode.Enabled = isChecked;
        }
        #endregion


        #region CheckUpdate

        private void tsbCheckUpdateN_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Global.UpdateUrl);
        }

        private void tsbCheckUpdateCore_Click(object sender, EventArgs e)
        {
            if (v2rayUpdateHandle == null)
            {
                v2rayUpdateHandle = new V2rayUpdateHandle();
                v2rayUpdateHandle.AbsoluteCompleted += (sender2, args) =>
                {
                    if (args.Success)
                    {
                        v2rayHandler_ProcessEvent(false, "解析V2rayCore成功！");

                        string url = args.Msg;
                        this.Invoke((MethodInvoker)(delegate
                        {
                            if (MessageBox.Show(this, "是否下载?\r\n" + url, "YesNo", MessageBoxButtons.YesNo) == DialogResult.No)
                            {
                                return;
                            }
                            else
                            {
                                v2rayUpdateHandle.UpdateV2rayCore(config, url);
                            }
                        }));
                    }
                    else
                    {
                        v2rayHandler_ProcessEvent(false, args.Msg);
                    }
                };
                v2rayUpdateHandle.UpdateCompleted += (sender2, args) =>
                {
                    if (args.Success)
                    {
                        v2rayHandler_ProcessEvent(false, "下载V2rayCore成功！");
                        v2rayHandler_ProcessEvent(false, "正在解压......");

                        try
                        {
                            CloseV2ray();

                            string fileName = args.Msg;
                            fileName = Utils.GetPath(fileName);
                            using (ZipArchive archive = ZipFile.OpenRead(fileName))
                            {
                                foreach (ZipArchiveEntry entry in archive.Entries)
                                {
                                    if (entry.Length == 0)
                                        continue;
                                    entry.ExtractToFile(Utils.GetPath(entry.Name), true);
                                }
                            }
                            v2rayHandler_ProcessEvent(false, "更新V2rayCore成功！正在重启服务...");

                            Global.reloadV2ray = true;
                            LoadV2ray();

                            v2rayHandler_ProcessEvent(false, "更新V2rayCore成功！");
                        }
                        catch (Exception ex)
                        {
                            v2rayHandler_ProcessEvent(false, ex.Message);
                        }
                    }
                    else
                    {
                        v2rayHandler_ProcessEvent(false, args.Msg);
                    }
                };
                v2rayUpdateHandle.Error += (sender2, args) =>
                {
                    v2rayHandler_ProcessEvent(true, args.GetException().Message);
                };
            }

            v2rayHandler_ProcessEvent(false, "开始更新V2rayCore...");
            v2rayUpdateHandle.AbsoluteV2rayCore(config);
        }

        private void tsbCheckUpdatePACList_Click(object sender, EventArgs e)
        {
            if (pacListHandle == null)
            {
                pacListHandle = new PACListHandle();
                pacListHandle.UpdateCompleted += (sender2, args) =>
                {
                    if (args.Success)
                    {
                        v2rayHandler_ProcessEvent(false, "PAC更新成功！");
                    }
                    else
                    {
                        v2rayHandler_ProcessEvent(false, "PAC更新失败！");
                    }
                };
                pacListHandle.Error += (sender2, args) =>
                {
                    v2rayHandler_ProcessEvent(true, args.GetException().Message);
                };
            }
            v2rayHandler_ProcessEvent(false, "开始更新PAC...");
            pacListHandle.UpdatePACFromGFWList(config);
        }

        #endregion

    }
}
