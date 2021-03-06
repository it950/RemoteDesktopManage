﻿using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using MetroFramework.Controls;
using MetroFramework.Forms;
using MetroFramework.MessageBox;
using Newtonsoft.Json;
using RdpTest.Model;

namespace RdpTest
{
    public partial class RemoteHostForm : MetroForm
    {
        public RemoteHostForm()
        {
            InitializeComponent();
        }

        public RemoteHost RemoteHost { get; set; }

        private bool IsModify => RemoteHost != null;

        private void RemoteHostForm_Load(object sender, System.EventArgs e)
        {
            //设置主题
            chIsParent.StyleManager = StyleManager;
            chShareClipboard.StyleManager = StyleManager;
            chShareAllDisk.StyleManager = StyleManager;

            //共享设置
            chShareClipboard.Checked = GlobalConfig.Instance.ShareClipboard;
            chShareAllDisk.CheckedChanged += (o, args) => gbDisks.Enabled = !chShareAllDisk.Checked;
            chShareAllDisk.Checked = GlobalConfig.Instance.ShareAllDisk;

            //可用磁盘
            flpDisks.Controls.Clear();
            foreach (var driveInfo in DriveInfo.GetDrives())
            {
                flpDisks.Controls.Add(new MetroCheckBox
                {
                    Text = driveInfo.Name,
                    AutoSize = true,
                    StyleManager = StyleManager,
                    Checked = GlobalConfig.Instance.ShareDiskList.Contains(driveInfo.Name)
                });
            }

            //取消显示上下按钮
            numSort.Controls.RemoveAt(0);

            //加载父节点下拉框信息
            LoadParentComboBox();

            //是否需要加载信息
            if (IsModify)
                LoadRemoteHost();
            else
                txtName.Select();
        }

        private void LoadParentComboBox()
        {
            var parents = Db.Connection.Query<RemoteHost>("SELECT Id,Name FROM RemoteHost WHERE ParentId=0 ORDER BY Sort");
            cbParent.DisplayMember = "Name";
            cbParent.ValueMember = "Id";
            cbParent.DataSource = parents;
        }

        private void LoadRemoteHost()
        {
            lbStatus.Text = "[修改]";

            chIsParent.Checked = RemoteHost.ParentId == 0;
            chIsParent.Enabled = false; //不允许修改节点类型

            cbParent.SelectedValue = RemoteHost.ParentId;
            txtName.Text = RemoteHost.Name;
            numSort.Value = RemoteHost.Sort;

            txtAddress.Text = RemoteHost.Address;
            numPort.Value = RemoteHost.Port;
            txtUser.Text = RemoteHost.User;
            txtPwd.Text = RemoteHost.Pwd;
            txtRemoteProgram.Text = RemoteHost.RemoteProgram;

            chShareClipboard.Checked = RemoteHost.Ext.ShareClipboard;
            chShareAllDisk.Checked = RemoteHost.Ext.ShareAllDisk;
            foreach (var ch in flpDisks.Controls.OfType<MetroCheckBox>())
            {
                ch.Checked = RemoteHost.Ext.ShareDiskList.Contains(ch.Text);
            }

            lbCopy.Visible = true; //显示复制按钮
        }

        private void lbCopy_Click(object sender, EventArgs e)
        {
            RemoteHost = null;
            lbCopy.Visible = false;
            lbStatus.Text = "[新增]";
            chIsParent.Enabled = true;
        }

        private void cbIsParent_CheckedChanged(object sender, EventArgs e)
        {
            cbParent.Visible = pnlHost.Visible = !chIsParent.Checked;

            if (chIsParent.Checked)
            {
                Height -= pnlHost.Height;
            }
            else
            {
                Height += pnlHost.Height;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            txtName.Text = txtName.Text.Trim();
            if (txtName.TextLength <= 0)
            {
                MetroMessageBox.Show(this, "请填写名称！");
                txtName.Focus();
                txtName.SelectAll();
                return;
            }
            if (!chIsParent.Checked && cbParent.SelectedIndex < 0)
            {
                MetroMessageBox.Show(this, "请选择父级！");
                cbParent.DroppedDown = true;
                return;
            }

            var host = new RemoteHost
            {
                Name = txtName.Text,
                Sort = Convert.ToInt32(numSort.Value),
            };
            if (!chIsParent.Checked)
            {
                host.ParentId = Convert.ToInt32(cbParent.SelectedValue);
                host.Address = txtAddress.Text.Trim();
                host.Port = Convert.ToInt32(numPort.Value);
                host.User = txtUser.Text.Trim();
                host.Pwd = txtPwd.Text;
                host.RemoteProgram = txtRemoteProgram.Text.Trim();

                host.Ext.ShareClipboard = chShareClipboard.Checked;
                host.Ext.ShareAllDisk = chShareAllDisk.Checked;
                host.Ext.ShareDiskList = flpDisks.Controls.OfType<MetroCheckBox>().Where(ch => ch.Checked).Select(ch => ch.Text).ToList();

                host.ExtJson = JsonConvert.SerializeObject(host.Ext);
            }

            Db.Connection.Execute(
                IsModify
                    ? @"UPDATE RemoteHost SET Name=@Name,Address=@Address,Port=@Port,User=@User,Pwd=@Pwd,Sort=@Sort,ParentId=@ParentId,
                        RemoteProgram=@RemoteProgram,ExtJson=@ExtJson WHERE Id=" + RemoteHost.Id

                    : @"INSERT INTO RemoteHost(Name,Address,Port,User,Pwd,Sort,ParentId,RemoteProgram,ExtJson) 
                        VALUES(@Name,@Address,@Port,@User,@Pwd,@Sort,@ParentId,@RemoteProgram,@ExtJson)",
                host);


            DialogResult = DialogResult.OK;
        }


    }
}
