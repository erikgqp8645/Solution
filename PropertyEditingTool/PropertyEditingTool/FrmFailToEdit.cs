using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MD_SW_ConnectSW;
using PropertyEditingTool.Models;

namespace PropertyEditingTool;

public class FrmFailToEdit : Form
{
	public Action<string> delegatCheckEditResult;

	public List<SwProperty> listFailure;

	private SldWorkService sw = new SldWorkService();

	private IContainer components;

	private DataGridView dataGridView;

	private Button btnContinue;

	private CheckBox chkAllSelect;

	private DataGridViewCheckBoxColumn Checked;

	private DataGridViewTextBoxColumn FileName;

	private DataGridViewTextBoxColumn ConfigName;

	private DataGridViewTextBoxColumn ErrorInfo;

	private DataGridViewTextBoxColumn Old_Name;

	private DataGridViewTextBoxColumn Old_Value;

	private DataGridViewTextBoxColumn New_Name;

	private DataGridViewTextBoxColumn New_Value;

	private DataGridViewTextBoxColumn EditType;

	public FrmFailToEdit()
	{
		InitializeComponent();
		dataGridView.AutoGenerateColumns = false;
		dataGridView.AllowUserToAddRows = false;
	}

	private void FrmTip_Load(object sender, EventArgs e)
	{
		dataGridView.DataSource = listFailure;
		chkAllSelect.Checked = true;
	}

	private void btnContinue_Click(object sender, EventArgs e)
	{
		foreach (SwProperty swProp in listFailure)
		{
			if (swProp.EditType == null)
			{
				swProp.ErrorInfo = "您的修改方案不支持，请重新输入。";
			}
		}
		try
		{
			sw.swApp = ConnectSW.iSwApp;
			if (sw.swApp == null)
			{
				return;
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "温馨提示");
			return;
		}
		Thread thread = new Thread((ThreadStart)delegate
		{
			List<SwProperty> list = listFailure.FindAll((SwProperty prop) => prop.Checked);
			if (list.Count == 0)
			{
				MessageBox.Show("请输勾选您要继续修改的模型", "温馨提示");
			}
			else
			{
				foreach (IGrouping<string, SwProperty> file2 in from file in list
					group file by file.FullName)
				{
					sw.OpenDoc(new SwFile(file2.Key));
					foreach (SwProperty current in file2)
					{
						int index = listFailure.IndexOf(current);
						dataGridView.Invoke((Action)delegate
						{
							dataGridView.CurrentCell = dataGridView.Rows[index].Cells[0];
						});
						if (current.Checked)
						{
							if (current.ConfigName == "自定义属性")
							{
								sw.EditCustomInfo(current);
							}
							else
							{
								sw.EditConfigProp(current);
							}
						}
						Invoke((Action)delegate
						{
							if (listFailure[index].ErrorInfo == "修改成功")
							{
								dataGridView.Rows[index].DefaultCellStyle.BackColor = Color.YellowGreen;
							}
							else
							{
								dataGridView.Rows[index].DefaultCellStyle.BackColor = Color.White;
							}
							dataGridView.Refresh();
						});
					}
					sw.CloseDoc(isSave: true);
					Invoke((Action)delegate
					{
						delegatCheckEditResult(file2.Key);
					});
				}
				MessageBox.Show("执行完成", "提示");
			}
		});
		thread.IsBackground = false;
		thread.Start();
	}

	private void dataGridView_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
	{
		if (e.Button == MouseButtons.Right && e.ColumnIndex > -1 && e.RowIndex > -1)
		{
			dataGridView.ClearSelection();
			dataGridView.Rows[e.RowIndex].Selected = true;
			List<string> obj = new List<string> { "打开模型", "打开目录" };
			List<MouseEventHandler> listEventHandler = new List<MouseEventHandler> { TsmOpenModel_MouseUp, TsmOpenDir_MouseUp };
			ControlHelper.NewContextMenuStrip(obj, listEventHandler).Show(Control.MousePosition.X, Control.MousePosition.Y);
		}
	}

	private void TsmOpenModel_MouseUp(object sender, MouseEventArgs e)
	{
		try
		{
			int index = dataGridView.SelectedRows[0].Index;
			sw.swApp = ConnectSW.iSwApp;
			if (sw.swApp != null)
			{
				sw.OpenDoc(new SwFile(listFailure[index].FullName));
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "温馨提示");
		}
	}

	private void TsmOpenDir_MouseUp(object sender, MouseEventArgs e)
	{
		try
		{
			int index = dataGridView.SelectedRows[0].Index;
			Process.Start(Path.GetDirectoryName(listFailure[index].FullName));
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "温馨提示");
		}
	}

	private void cbAllSelect_CheckedChanged(object sender, EventArgs e)
	{
		if (chkAllSelect.Checked)
		{
			for (int i = 0; i < dataGridView.Rows.Count; i++)
			{
				if (dataGridView.Rows[i].Cells["ErrorInfo"].Value.ToString() != "修改成功")
				{
					dataGridView.Rows[i].Cells["Checked"].Value = "True";
				}
			}
		}
		else
		{
			for (int j = 0; j < dataGridView.Rows.Count; j++)
			{
				dataGridView.Rows[j].Cells["Checked"].Value = "False";
			}
		}
	}

	private void dataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
	{
		dataGridView.Refresh();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.dataGridView = new System.Windows.Forms.DataGridView();
		this.Checked = new System.Windows.Forms.DataGridViewCheckBoxColumn();
		this.FileName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.ConfigName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.ErrorInfo = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.Old_Name = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.Old_Value = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.New_Name = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.New_Value = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.EditType = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.btnContinue = new System.Windows.Forms.Button();
		this.chkAllSelect = new System.Windows.Forms.CheckBox();
		((System.ComponentModel.ISupportInitialize)this.dataGridView).BeginInit();
		base.SuspendLayout();
		this.dataGridView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridView.Columns.AddRange(this.Checked, this.FileName, this.ConfigName, this.ErrorInfo, this.Old_Name, this.Old_Value, this.New_Name, this.New_Value, this.EditType);
		this.dataGridView.Location = new System.Drawing.Point(0, 0);
		this.dataGridView.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
		this.dataGridView.Name = "dataGridView";
		this.dataGridView.RowTemplate.Height = 23;
		this.dataGridView.Size = new System.Drawing.Size(1263, 597);
		this.dataGridView.TabIndex = 0;
		this.dataGridView.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(dataGridView_CellEndEdit);
		this.dataGridView.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(dataGridView_CellMouseClick);
		this.Checked.DataPropertyName = "Checked";
		this.Checked.HeaderText = "全选";
		this.Checked.Name = "Checked";
		this.Checked.Resizable = System.Windows.Forms.DataGridViewTriState.True;
		this.Checked.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
		this.Checked.Width = 55;
		this.FileName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
		this.FileName.DataPropertyName = "Name";
		this.FileName.HeaderText = "文件名称";
		this.FileName.Name = "FileName";
		this.FileName.ReadOnly = true;
		this.ConfigName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
		this.ConfigName.DataPropertyName = "ConfigName";
		this.ConfigName.HeaderText = "配置名";
		this.ConfigName.Name = "ConfigName";
		this.ConfigName.ReadOnly = true;
		this.ErrorInfo.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
		this.ErrorInfo.DataPropertyName = "ErrorInfo";
		this.ErrorInfo.FillWeight = 150f;
		this.ErrorInfo.HeaderText = "错误信息";
		this.ErrorInfo.Name = "ErrorInfo";
		this.ErrorInfo.ReadOnly = true;
		this.Old_Name.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
		this.Old_Name.DataPropertyName = "Old_Name";
		this.Old_Name.HeaderText = "原名称";
		this.Old_Name.Name = "Old_Name";
		this.Old_Value.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
		this.Old_Value.DataPropertyName = "Old_Value";
		this.Old_Value.HeaderText = "原值";
		this.Old_Value.Name = "Old_Value";
		this.New_Name.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
		this.New_Name.DataPropertyName = "New_Name";
		this.New_Name.HeaderText = "新名称";
		this.New_Name.Name = "New_Name";
		this.New_Value.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
		this.New_Value.DataPropertyName = "New_Value";
		this.New_Value.HeaderText = "新值";
		this.New_Value.Name = "New_Value";
		this.EditType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
		this.EditType.DataPropertyName = "EditType";
		this.EditType.HeaderText = "修改方案";
		this.EditType.Name = "EditType";
		this.EditType.ReadOnly = true;
		this.btnContinue.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.btnContinue.Font = new System.Drawing.Font("微软雅黑", 10.5f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
		this.btnContinue.Location = new System.Drawing.Point(1164, 598);
		this.btnContinue.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
		this.btnContinue.Name = "btnContinue";
		this.btnContinue.Size = new System.Drawing.Size(100, 32);
		this.btnContinue.TabIndex = 1;
		this.btnContinue.Text = "继续执行";
		this.btnContinue.UseVisualStyleBackColor = true;
		this.btnContinue.Click += new System.EventHandler(btnContinue_Click);
		this.chkAllSelect.AutoSize = true;
		this.chkAllSelect.BackColor = System.Drawing.SystemColors.ButtonHighlight;
		this.chkAllSelect.Font = new System.Drawing.Font("微软雅黑", 7.5f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
		this.chkAllSelect.Location = new System.Drawing.Point(76, 6);
		this.chkAllSelect.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.chkAllSelect.Name = "chkAllSelect";
		this.chkAllSelect.Size = new System.Drawing.Size(15, 14);
		this.chkAllSelect.TabIndex = 3;
		this.chkAllSelect.UseVisualStyleBackColor = false;
		this.chkAllSelect.CheckedChanged += new System.EventHandler(cbAllSelect_CheckedChanged);
		base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 17f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(1264, 631);
		base.Controls.Add(this.chkAllSelect);
		base.Controls.Add(this.btnContinue);
		base.Controls.Add(this.dataGridView);
		this.Font = new System.Drawing.Font("微软雅黑", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
		base.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
		base.Name = "FrmFailToEdit";
		this.Text = "修改异常列表";
		base.Load += new System.EventHandler(FrmTip_Load);
		((System.ComponentModel.ISupportInitialize)this.dataGridView).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
