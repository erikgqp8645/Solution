using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MD_SW_ConnectSW;
using PropertyEditingTool.Models;
using PropertyEditingTool.Properties;

namespace PropertyEditingTool;

public class FrmAddDrwNum : Form
{
	public List<SwFile> listSwDrw;

	public string drwNumName;

	public string startNum;

	public Action<string, string> SetConfigDelegate;

	private SldWorkService sw = new SldWorkService();

	private IContainer components;

	private Button btnAddDrw;

	private Label label1;

	private Label label2;

	private ComboBox cboPropName;

	private TextBox txtStartNum;

	private Label label3;

	private Label label4;

	private Button btnWrite;

	private DataGridView dataGridView;

	private DataGridViewTextBoxColumn FileTypeEx;

	private DataGridViewTextBoxColumn ModelName;

	private DataGridViewTextBoxColumn DrwNumValue;

	private DataGridViewTextBoxColumn EditResult;

	private CheckBox chkCurrentConfig;

	private CheckBox chkCustomize;

	private CheckBox chkWriteModel;

	private CheckBox chkWriteDrw;

	private Label label5;

	private NumericUpDown numericUpDown;

	public FrmAddDrwNum()
	{
		InitializeComponent();
		base.Icon = Resources.app;
		dataGridView.AutoGenerateColumns = false;
		dataGridView.AllowUserToAddRows = false;
	}

	private void FrmAddDrwNum_Load(object sender, EventArgs e)
	{
		cboPropName.Text = Settings.Default["DrawNumName"].ToString();
		txtStartNum.Text = Settings.Default["StartNumber"].ToString();
		chkCurrentConfig.Checked = (bool)Settings.Default["IsWriteConfig"];
		chkWriteModel.Checked = (bool)Settings.Default["IsWriteCustom"];
		chkCustomize.Checked = (bool)Settings.Default["IsWriteCorresModel"];
		chkWriteDrw.Checked = (bool)Settings.Default["IsWriteDrawDoc"];
		txtStartNum.Text = startNum;
		if (listSwDrw.Count == 0)
		{
			return;
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
		if (AddFileTypeEx())
		{
			txtStartNum_Leave(null, null);
			dataGridView.DataSource = listSwDrw;
		}
	}

	private void btnAddDrw_Click(object sender, EventArgs e)
	{
		string dir = ControlHelper.OpenfolderDialog();
		if (dir != null)
		{
			List<string> files = new List<string>();
			ControlHelper.GetSubFile(dir, 0, ref files, "*SLDDRW");
			string[] files2 = files.Where((string file) => Path.GetFileName(file)[0] != '~').ToArray();
			List<string> listFile = listSwDrw.Select((SwFile swDrw) => swDrw.FullName).ToList();
			listFile.AddRange(files2);
			listFile = listFile.Distinct().ToList();
			listSwDrw = listFile.Select((string file) => new SwFile(file)).ToList();
			if (AddFileTypeEx())
			{
				dataGridView.DataSource = null;
				dataGridView.DataSource = listSwDrw;
				txtStartNum_Leave(null, null);
			}
		}
	}

	public bool AddFileTypeEx()
	{
		try
		{
			sw.swApp = ConnectSW.iSwApp;
			if (sw.swApp == null)
			{
				return false;
			}
			foreach (SwFile drw2 in listSwDrw)
			{
				dynamic reference = sw.swApp.GetDocumentDependencies2(drw2.FullName, Traverseflag: false, Searchflag: true, AddReadOnlyInfo: false);
				if (reference == null)
				{
					MessageBox.Show("您的选择的图纸版本过高，当前SldWorks版本无法打开。", "温馨提示");
					return false;
				}
				string modelName = reference[1];
				if (Path.GetExtension(modelName).ToUpper() == ".SLDPRT")
				{
					drw2.FileTypeEx = "零件图纸";
				}
				else if (Path.GetExtension(modelName).ToUpper() == ".SLDASM")
				{
					drw2.FileTypeEx = "装配图纸";
				}
			}
			listSwDrw = listSwDrw.OrderByDescending((SwFile drw) => drw.FileTypeEx).ToList();
			return true;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "温馨提示");
			return false;
		}
	}

	private void txtStartNum_Leave(object sender, EventArgs e)
	{
		if (!DataValidate.IsFun(txtStartNum.Text.Trim()))
		{
			MessageBox.Show("您输入的编码规则有误：为包含“{数字}”样式。", "提示");
			txtStartNum.Focus();
			return;
		}
		string startNum = DataValidate.GetStartNum(txtStartNum.Text.Trim());
		string startNum_str = startNum.Trim("{ }".ToCharArray());
		int startNum_int = Convert.ToInt32(startNum_str);
		int Increment = Convert.ToInt32(numericUpDown.Value);
		string format = string.Empty;
		for (int i = 0; i < startNum_str.Length; i++)
		{
			format += "0";
		}
		foreach (SwFile item in listSwDrw)
		{
			item.DrwNumValue = txtStartNum.Text.Trim().Replace(startNum, startNum_int.ToString(format));
			startNum_int += Increment;
		}
		dataGridView.Refresh();
	}

	private void cboPropName_TextChanged(object sender, EventArgs e)
	{
		dataGridView.Columns["DrwNumValue"].HeaderText = cboPropName.Text;
	}

	private void dataGridView_DragDrop(object sender, DragEventArgs e)
	{
		string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
		if (files == null)
		{
			int idx = GetRowFromPoint(e.X, e.Y);
			if (idx < 0)
			{
				return;
			}
			DataGridViewRow row = (DataGridViewRow)e.Data.GetData(typeof(DataGridViewRow));
			SwFile objSwDrw = listSwDrw[row.Index];
			listSwDrw.RemoveAt(row.Index);
			if (idx == dataGridView.RowCount)
			{
				idx--;
			}
			listSwDrw.Insert(idx, objSwDrw);
			txtStartNum_Leave(null, null);
		}
		List<string> listFile = new List<string>();
		string[] array = files;
		foreach (string file in array)
		{
			if (Path.GetExtension(file).ToUpper() == ".SLDDRW")
			{
				listFile.Add(file);
			}
		}
		AddSwFile(listFile.ToArray());
		if (AddFileTypeEx())
		{
			dataGridView.DataSource = null;
			dataGridView.DataSource = listSwDrw;
			txtStartNum_Leave(null, null);
		}
	}

	public void AddSwFile(string[] files)
	{
		List<string> listFile = listSwDrw.Select((SwFile swFile) => swFile.FullName).ToList();
		listFile.AddRange(files);
		listFile = listFile.Distinct().ToList();
		listSwDrw = listFile.Select((string file) => new SwFile(file)).ToList();
	}

	private int GetRowFromPoint(int x, int y)
	{
		for (int i = 0; i < dataGridView.RowCount; i++)
		{
			Rectangle rec = dataGridView.GetRowDisplayRectangle(i, cutOverflow: false);
			if (dataGridView.RectangleToScreen(rec).Contains(x, y))
			{
				return i;
			}
		}
		return -1;
	}

	private void dataGridView_DragEnter(object sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			e.Effect = DragDropEffects.Copy;
		}
		else
		{
			e.Effect = DragDropEffects.Move;
		}
	}

	private void dataGridView_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left && e.ColumnIndex < 0 && dataGridView.CurrentRow.Index < dataGridView.RowCount - 1)
		{
			dataGridView.DoDragDrop(dataGridView.Rows[e.RowIndex], DragDropEffects.Move);
		}
	}

	private void dataGridView_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
	{
		if (e.Button == MouseButtons.Right && e.ColumnIndex > -1 && e.RowIndex > -1)
		{
			dataGridView.ClearSelection();
			dataGridView.Rows[e.RowIndex].Selected = true;
			List<string> obj = new List<string> { "移除选择项", "清空全部项", "打开图纸" };
			List<MouseEventHandler> listEventHandler = new List<MouseEventHandler> { TsmRemoveDrw_MouseUp, TsmClearDrw_MouseUp, TsmOpenDrw_MouseUp };
			ControlHelper.NewContextMenuStrip(obj, listEventHandler).Show(Control.MousePosition.X, Control.MousePosition.Y);
		}
	}

	private void TsmOpenDrw_MouseUp(object sender, MouseEventArgs e)
	{
		try
		{
			int index = dataGridView.SelectedRows[0].Index;
			sw.swApp = ConnectSW.iSwApp;
			if (sw.swApp != null)
			{
				sw.OpenDoc(listSwDrw[index]);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "温馨提示");
		}
	}

	private void TsmRemoveDrw_MouseUp(object sender, MouseEventArgs e)
	{
		try
		{
			int index = dataGridView.SelectedRows[0].Index;
			listSwDrw.RemoveAt(index);
			dataGridView.DataSource = null;
			dataGridView.DataSource = listSwDrw;
			txtStartNum_Leave(null, null);
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "温馨提示");
		}
	}

	private void TsmClearDrw_MouseUp(object sender, MouseEventArgs e)
	{
		try
		{
			listSwDrw.Clear();
			dataGridView.DataSource = null;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "温馨提示");
		}
	}

	private void btnWrite_Click(object sender, EventArgs e)
	{
		SavaSetting();
		if (listSwDrw.Count == 0)
		{
			return;
		}
		if (!chkCustomize.Checked && !chkCurrentConfig.Checked)
		{
			MessageBox.Show("请选择要将图号写入配置特定或是自定义属性", "提示");
			return;
		}
		if (!chkWriteModel.Checked && !chkWriteDrw.Checked)
		{
			MessageBox.Show("请选择要将图号写入模型或是图纸", "提示");
			return;
		}
		drwNumName = cboPropName.Text.Trim();
		Thread thread = new Thread((ThreadStart)delegate
		{
			try
			{
				foreach (SwFile swFile in listSwDrw)
				{
					int index = listSwDrw.IndexOf(swFile);
					dataGridView.Invoke((Action)delegate
					{
						dataGridView.CurrentCell = dataGridView.Rows[index].Cells[0];
					});
					sw.OpenDoc(swFile);
					swFile.DrwNumName = drwNumName;
					sw.AddDrwNum(swFile, chkCustomize.Checked, chkCurrentConfig.Checked, chkWriteModel.Checked, chkWriteDrw.Checked);
					Invoke((Action)delegate
					{
						if (swFile.EditResult == "已修改")
						{
							dataGridView.Rows[index].DefaultCellStyle.BackColor = Color.YellowGreen;
						}
						else
						{
							dataGridView.Rows[index].DefaultCellStyle.BackColor = Color.White;
						}
						dataGridView.Refresh();
					});
					sw.CloseDoc(isSave: true);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("执行失败：" + ex.Message, "提示");
			}
			if (listSwDrw.Count((SwFile drw) => drw.EditResult != "已修改") == 0)
			{
				MessageBox.Show("全部添加成功！", "提示");
			}
		});
		thread.IsBackground = false;
		thread.Start();
	}

	private void SavaSetting()
	{
		string startNumber = txtStartNum.Text;
		bool isWriteConfig = chkCurrentConfig.Checked;
		bool isWriteCustom = chkWriteModel.Checked;
		bool isWriteCorresModel = chkCustomize.Checked;
		bool isWriteDrawDoc = chkWriteDrw.Checked;
		Settings.Default["DrawNumName"] = cboPropName.Text;
		Settings.Default["StartNumber"] = startNumber;
		Settings.Default["IsWriteConfig"] = isWriteConfig;
		Settings.Default["IsWriteCustom"] = isWriteCustom;
		Settings.Default["IsWriteCorresModel"] = isWriteCorresModel;
		Settings.Default["IsWriteDrawDoc"] = isWriteDrawDoc;
		Settings.Default.Save();
	}

	private void chkWriteDrw_CheckedChanged(object sender, EventArgs e)
	{
		if (chkWriteDrw.Checked && !chkWriteModel.Checked)
		{
			chkCurrentConfig.Checked = false;
			chkCustomize.Checked = true;
		}
	}

	private void FrmAddDrwNum_FormClosing(object sender, FormClosingEventArgs e)
	{
		SetConfigDelegate(cboPropName.Text.Trim(), txtStartNum.Text.Trim());
	}

	private void numericUpDown_ValueChanged(object sender, EventArgs e)
	{
		txtStartNum_Leave(null, null);
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
		this.btnAddDrw = new System.Windows.Forms.Button();
		this.label1 = new System.Windows.Forms.Label();
		this.label2 = new System.Windows.Forms.Label();
		this.cboPropName = new System.Windows.Forms.ComboBox();
		this.txtStartNum = new System.Windows.Forms.TextBox();
		this.label3 = new System.Windows.Forms.Label();
		this.label4 = new System.Windows.Forms.Label();
		this.btnWrite = new System.Windows.Forms.Button();
		this.dataGridView = new System.Windows.Forms.DataGridView();
		this.FileTypeEx = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.ModelName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.DrwNumValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.EditResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.chkCurrentConfig = new System.Windows.Forms.CheckBox();
		this.chkCustomize = new System.Windows.Forms.CheckBox();
		this.chkWriteModel = new System.Windows.Forms.CheckBox();
		this.chkWriteDrw = new System.Windows.Forms.CheckBox();
		this.label5 = new System.Windows.Forms.Label();
		this.numericUpDown = new System.Windows.Forms.NumericUpDown();
		((System.ComponentModel.ISupportInitialize)this.dataGridView).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.numericUpDown).BeginInit();
		base.SuspendLayout();
		this.btnAddDrw.BackColor = System.Drawing.Color.FromArgb(224, 224, 224);
		this.btnAddDrw.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnAddDrw.Location = new System.Drawing.Point(6, 5);
		this.btnAddDrw.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
		this.btnAddDrw.Name = "btnAddDrw";
		this.btnAddDrw.Size = new System.Drawing.Size(100, 30);
		this.btnAddDrw.TabIndex = 0;
		this.btnAddDrw.Text = "加载图纸";
		this.btnAddDrw.UseVisualStyleBackColor = false;
		this.btnAddDrw.Click += new System.EventHandler(btnAddDrw_Click);
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(12, 43);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(93, 20);
		this.label1.TabIndex = 2;
		this.label1.Text = "图号属性名：";
		this.label2.AutoSize = true;
		this.label2.Location = new System.Drawing.Point(250, 44);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(79, 20);
		this.label2.TabIndex = 3;
		this.label2.Text = "起始编号：";
		this.cboPropName.FormattingEnabled = true;
		this.cboPropName.Items.AddRange(new object[3] { "图号", "代号", "零件代号" });
		this.cboPropName.Location = new System.Drawing.Point(100, 39);
		this.cboPropName.Name = "cboPropName";
		this.cboPropName.Size = new System.Drawing.Size(120, 28);
		this.cboPropName.TabIndex = 1;
		this.cboPropName.TextChanged += new System.EventHandler(cboPropName_TextChanged);
		this.txtStartNum.Location = new System.Drawing.Point(325, 41);
		this.txtStartNum.Name = "txtStartNum";
		this.txtStartNum.Size = new System.Drawing.Size(140, 26);
		this.txtStartNum.TabIndex = 2;
		this.txtStartNum.Text = "Md-{000}";
		this.txtStartNum.Leave += new System.EventHandler(txtStartNum_Leave);
		this.label3.AutoSize = true;
		this.label3.Location = new System.Drawing.Point(472, 44);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(79, 20);
		this.label3.TabIndex = 7;
		this.label3.Text = "编号增量：";
		this.label4.AutoSize = true;
		this.label4.Location = new System.Drawing.Point(250, 10);
		this.label4.Name = "label4";
		this.label4.Size = new System.Drawing.Size(322, 20);
		this.label4.TabIndex = 8;
		this.label4.Text = "编码规则：如 Md-{000}，括号内表示编码位数3位";
		this.btnWrite.BackColor = System.Drawing.Color.FromArgb(192, 255, 192);
		this.btnWrite.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnWrite.Location = new System.Drawing.Point(119, 5);
		this.btnWrite.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
		this.btnWrite.Name = "btnWrite";
		this.btnWrite.Size = new System.Drawing.Size(100, 30);
		this.btnWrite.TabIndex = 10;
		this.btnWrite.Text = "开始写入";
		this.btnWrite.UseVisualStyleBackColor = false;
		this.btnWrite.Click += new System.EventHandler(btnWrite_Click);
		this.dataGridView.AllowDrop = true;
		this.dataGridView.AllowUserToResizeColumns = false;
		this.dataGridView.AllowUserToResizeRows = false;
		this.dataGridView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.dataGridView.BackgroundColor = System.Drawing.Color.FromArgb(239, 255, 255);
		this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridView.Columns.AddRange(this.FileTypeEx, this.ModelName, this.DrwNumValue, this.EditResult);
		this.dataGridView.Location = new System.Drawing.Point(4, 72);
		this.dataGridView.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
		this.dataGridView.Name = "dataGridView";
		this.dataGridView.RowTemplate.Height = 23;
		this.dataGridView.Size = new System.Drawing.Size(895, 460);
		this.dataGridView.TabIndex = 11;
		this.dataGridView.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(dataGridView_CellMouseClick);
		this.dataGridView.CellMouseMove += new System.Windows.Forms.DataGridViewCellMouseEventHandler(dataGridView_CellMouseMove);
		this.dataGridView.DragDrop += new System.Windows.Forms.DragEventHandler(dataGridView_DragDrop);
		this.dataGridView.DragEnter += new System.Windows.Forms.DragEventHandler(dataGridView_DragEnter);
		this.FileTypeEx.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
		this.FileTypeEx.DataPropertyName = "FileTypeEx";
		this.FileTypeEx.FillWeight = 50f;
		this.FileTypeEx.HeaderText = "文件类型";
		this.FileTypeEx.Name = "FileTypeEx";
		this.FileTypeEx.ReadOnly = true;
		this.ModelName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
		this.ModelName.DataPropertyName = "Name";
		this.ModelName.HeaderText = "文件名称";
		this.ModelName.Name = "ModelName";
		this.ModelName.ReadOnly = true;
		this.ModelName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
		this.DrwNumValue.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
		this.DrwNumValue.DataPropertyName = "DrwNumValue";
		this.DrwNumValue.FillWeight = 80f;
		this.DrwNumValue.HeaderText = "图号";
		this.DrwNumValue.Name = "DrwNumValue";
		this.EditResult.DataPropertyName = "EditResult";
		this.EditResult.HeaderText = "结果";
		this.EditResult.Name = "EditResult";
		this.EditResult.ReadOnly = true;
		this.EditResult.Width = 80;
		this.chkCurrentConfig.AutoSize = true;
		this.chkCurrentConfig.Checked = true;
		this.chkCurrentConfig.CheckState = System.Windows.Forms.CheckState.Checked;
		this.chkCurrentConfig.Location = new System.Drawing.Point(633, 9);
		this.chkCurrentConfig.Name = "chkCurrentConfig";
		this.chkCurrentConfig.Size = new System.Drawing.Size(112, 24);
		this.chkCurrentConfig.TabIndex = 6;
		this.chkCurrentConfig.Text = "写入配置特定";
		this.chkCurrentConfig.UseVisualStyleBackColor = true;
		this.chkCustomize.AutoSize = true;
		this.chkCustomize.Location = new System.Drawing.Point(633, 42);
		this.chkCustomize.Name = "chkCustomize";
		this.chkCustomize.Size = new System.Drawing.Size(126, 24);
		this.chkCustomize.TabIndex = 7;
		this.chkCustomize.Text = "写入自定义属性";
		this.chkCustomize.UseVisualStyleBackColor = true;
		this.chkWriteModel.AutoSize = true;
		this.chkWriteModel.Checked = true;
		this.chkWriteModel.CheckState = System.Windows.Forms.CheckState.Checked;
		this.chkWriteModel.Location = new System.Drawing.Point(771, 9);
		this.chkWriteModel.Name = "chkWriteModel";
		this.chkWriteModel.Size = new System.Drawing.Size(112, 24);
		this.chkWriteModel.TabIndex = 8;
		this.chkWriteModel.Text = "写入对应模型";
		this.chkWriteModel.UseVisualStyleBackColor = true;
		this.chkWriteDrw.AutoSize = true;
		this.chkWriteDrw.Location = new System.Drawing.Point(771, 42);
		this.chkWriteDrw.Name = "chkWriteDrw";
		this.chkWriteDrw.Size = new System.Drawing.Size(98, 24);
		this.chkWriteDrw.TabIndex = 9;
		this.chkWriteDrw.Text = "写入工程图";
		this.chkWriteDrw.UseVisualStyleBackColor = true;
		this.chkWriteDrw.CheckedChanged += new System.EventHandler(chkWriteDrw_CheckedChanged);
		this.label5.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.label5.AutoSize = true;
		this.label5.Location = new System.Drawing.Point(5, 536);
		this.label5.Name = "label5";
		this.label5.Size = new System.Drawing.Size(261, 20);
		this.label5.TabIndex = 61;
		this.label5.Text = "提示：左键拖动行标头可以调整列表顺序";
		this.numericUpDown.Location = new System.Drawing.Point(544, 41);
		this.numericUpDown.Minimum = new decimal(new int[4] { 1, 0, 0, 0 });
		this.numericUpDown.Name = "numericUpDown";
		this.numericUpDown.Size = new System.Drawing.Size(45, 26);
		this.numericUpDown.TabIndex = 3;
		this.numericUpDown.Value = new decimal(new int[4] { 1, 0, 0, 0 });
		this.numericUpDown.ValueChanged += new System.EventHandler(numericUpDown_ValueChanged);
		base.AutoScaleDimensions = new System.Drawing.SizeF(8f, 20f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.FromArgb(239, 255, 255);
		base.ClientSize = new System.Drawing.Size(904, 561);
		base.Controls.Add(this.numericUpDown);
		base.Controls.Add(this.label5);
		base.Controls.Add(this.chkWriteDrw);
		base.Controls.Add(this.chkWriteModel);
		base.Controls.Add(this.chkCustomize);
		base.Controls.Add(this.chkCurrentConfig);
		base.Controls.Add(this.dataGridView);
		base.Controls.Add(this.btnWrite);
		base.Controls.Add(this.label4);
		base.Controls.Add(this.txtStartNum);
		base.Controls.Add(this.cboPropName);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.label1);
		base.Controls.Add(this.btnAddDrw);
		base.Controls.Add(this.label3);
		this.Font = new System.Drawing.Font("微软雅黑", 10.5f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
		base.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
		base.Name = "FrmAddDrwNum";
		this.Text = "批量添加图号";
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(FrmAddDrwNum_FormClosing);
		base.Load += new System.EventHandler(FrmAddDrwNum_Load);
		((System.ComponentModel.ISupportInitialize)this.dataGridView).EndInit();
		((System.ComponentModel.ISupportInitialize)this.numericUpDown).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
