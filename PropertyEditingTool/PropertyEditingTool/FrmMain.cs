using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MD_SW_ConnectSW;
using PropertyEditingTool.Models;
using PropertyEditingTool.Properties;

namespace PropertyEditingTool;

public class FrmMain : Form
{
    private SldWorkService sw = new SldWorkService();

    // SwFile列表，用于存储SolidWorks文件
    private List<SwFile> listSwFile = new List<SwFile>();

    // SwProperty列表，用于存储SolidWorks文件的属性
    private List<SwProperty> listSwProp = new List<SwProperty>();

    private List<SwAddProperty> listSwAddProperty = new List<SwAddProperty>();//新增属性列表

    private Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);//配置文件

    private string drwNumName; //图纸编号名称

    private string startNum;

    private Thread thread;

    private IContainer components;

    private MenuStrip menuStrip; //菜单栏

    private ToolStripMenuItem toolStripMenuItem; //文件

    private ToolStripMenuItem btnAddFile;

    private ToolStripMenuItem btnAddDir;

    private ToolStripSeparator toolStripSeparator1;

    private ToolStripMenuItem btnActionFile;

    private ToolStripMenuItem btnSelectedItem;

    private ToolStripMenuItem tsBtnReduce;

    private ToolStripMenuItem tsBtnClear;

    private ToolStripMenuItem tsVideoHelp;

    private DataGridView dataGridView;

    private ToolTip toolTip;

    private GroupBox groupBox;

    private Button btnStart;

    private ToolStripMenuItem 移除全部零件ToolStripMenuItem;

    private ToolStripMenuItem 移除全部装配体ToolStripMenuItem;

    private ToolStripMenuItem 移除全部图纸ToolStripMenuItem;

    private ToolStripMenuItem 移除选择项ToolStripMenuItem;

    private ToolStripMenuItem btnAllOpenFile;

    private ToolStripMenuItem 移除全部模板文件ToolStripMenuItem;

    private Label label2;

    private DataGridViewTextBoxColumn modelName;

    private DataGridViewTextBoxColumn FileType;

    private DataGridViewTextBoxColumn EditResult;

    private TabPage tpAddFrame; //添加框架

    private TabPage tpPorpRemove; // 移除属性

    private TabPage tpPorpTransfer; // 转移属性

    private TabPage tpSeriesEdit;// 批量编辑

    private Label label9;

    private DataGridView dgvTransfer;

    private TabPage tpEditProp;

    private Label label6;

    private Button btnAddEditRules;

    private Label label5;

    private ComboBox cboEditRules;

    private DataGridView dgvEditProp;

    private DataGridView dgv_EditProp;

    private Label label4;

    private TabControl tabControl;

    private Label lblDirection;

    private Label label11;

    private Label label10;

    private RadioButton rdoListExcept;

    private RadioButton rdoList;

    private RadioButton rdoAllProp;

    private Label label12;

    private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;

    private Panel pnlType;

    private DataGridView dgvRemoveList;

    private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;

    private TextBox txtPropName;

    private Label label1;

    private Label label3;

    private Button btnAddDrwNum;

    private ComboBox cboNewValue;

    private CheckBox chkWriteDrw;

    private CheckBox chkWriteModel;

    private ToolStripMenuItem tsPDFHelp;

    private DataGridViewTextBoxColumn OldName;

    private DataGridViewTextBoxColumn OldValue;

    private DataGridViewTextBoxColumn NewName;

    private DataGridViewTextBoxColumn NewValue;

    private DataGridViewTextBoxColumn Rule;

    private DataGridViewTextBoxColumn Old_Name;

    private DataGridViewTextBoxColumn Old_Value;

    private DataGridViewTextBoxColumn New_Name;

    private DataGridViewTextBoxColumn New_Value;

    private DataGridViewTextBoxColumn EditType;

    private Label label7;

    private CheckBox chkCustomize;

    private CheckBox chkAllConfig;

    private CheckBox chkCurrentConfig;

    private ToolStripMenuItem btnAddAsmFile;

    private TabPage tbAddProperty;

    private DataGridView dgvAddProperty;

    private LinkLabel linkLabel1;

    private RadioButton rdbcover;

    private RadioButton rdbskip;

    private Label label8;

    private ComboBox cmbPropertyValue;

    private Button btnAddlist;

    private Label label13;

    private DataGridView dgvAddPropertyList;

    private DataGridViewTextBoxColumn propertyName;

    private DataGridViewTextBoxColumn propertyValue;

    private DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;

    private DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;

    public FrmMain()
    {
        InitializeComponent();
        InitializeAddProperty();
        base.Icon = Resources.app;
        dataGridView.AutoGenerateColumns = false;
        dataGridView.AllowUserToAddRows = false;
        dgvEditProp.AutoGenerateColumns = false;
        dgvEditProp.Rows.Add();
        dgvEditProp.AllowUserToAddRows = false;
        dgvAddProperty.AutoGenerateColumns = false;
        dgvAddProperty.Rows.Add();
        dgvAddProperty.AllowUserToAddRows = false;
        ComboBox.ObjectCollection items = cboNewValue.Items;
        object[] propValues2 = SwPropValue.propValues;
        items.AddRange(propValues2);
        List<string> rules = new List<string> { "原名 → 新值", "原值 → 新值", "（原名、原值） → 新值", "修改属性名", "添加属性", "关键字替换", "关键字替换2", "拷贝(新名 → 旧值)" };
        cboEditRules.DataSource = rules;
        cboEditRules.SelectedIndexChanged += CboEditRules_SelectedIndexChanged;
        cboEditRules.SelectedIndex = -1;
        cboEditRules.SelectedIndex = 0;
        dgv_EditProp.AutoGenerateColumns = false;
        dgv_EditProp.AllowUserToAddRows = false;
        dgv_EditProp.DataSource = new List<SwProperty>();
        dgvAddPropertyList.AutoGenerateColumns = false;
        dgvAddPropertyList.AllowUserToAddRows = false;
        dgvAddPropertyList.DataSource = new List<SwAddProperty>();
        tabControl_SelectedIndexChanged(null, null);
        try
        {
            txtPropName.Text = config.AppSettings.Settings["FramePropName"].Value;
            List<string> transferPropName = new List<string>();
            string[] allKeys = config.AppSettings.Settings.AllKeys;
            foreach (string key4 in allKeys)
            {
                if (key4.Contains("Transfer"))
                {
                    string value2 = config.AppSettings.Settings[key4].Value;
                    if (value2.Trim().Length > 0)
                    {
                        transferPropName.Add(value2);
                    }
                }
            }
            dgvTransfer.RowCount = transferPropName.Count + 1;
            for (int k = 0; k < transferPropName.Count; k++)
            {
                dgvTransfer.Rows[k].Cells[0].Value = transferPropName[k];
            }
            List<string> removePropName = new List<string>();
            allKeys = config.AppSettings.Settings.AllKeys;
            foreach (string key3 in allKeys)
            {
                if (key3.Contains("Remove"))
                {
                    string value = config.AppSettings.Settings[key3].Value;
                    if (value.Trim().Length > 0)
                    {
                        removePropName.Add(value);
                    }
                }
            }
            dgvRemoveList.RowCount = removePropName.Count + 1;
            for (int j = 0; j < removePropName.Count; j++)
            {
                dgvRemoveList.Rows[j].Cells[0].Value = removePropName[j];
            }
            List<string> propNames = new List<string>();
            allKeys = config.AppSettings.Settings.AllKeys;
            foreach (string key2 in allKeys)
            {
                if (key2.Contains("AddPropertyName"))
                {
                    string propertyName = config.AppSettings.Settings[key2].Value;
                    if (propertyName.Trim().Length > 0)
                    {
                        propNames.Add(propertyName);
                    }
                }
            }
            List<string> propValues = new List<string>();
            allKeys = config.AppSettings.Settings.AllKeys;
            foreach (string key in allKeys)
            {
                if (key.Contains("AddPropertyValue"))
                {
                    string propertyValue = config.AppSettings.Settings[key].Value;
                    if (propertyValue.Trim().Length > 0)
                    {
                        propValues.Add(propertyValue);
                    }
                }
            }
            for (int i = 0; i < propNames.Count; i++)
            {
                listSwAddProperty.Add(new SwAddProperty(propNames[i], propValues[i]));
            }
            if (listSwAddProperty.Count != 0)
            {
                dgvAddPropertyList.DataSource = listSwAddProperty;
            }
            if (config.AppSettings.Settings["PropExistence"].Value == "0")
            {
                rdbcover.Checked = true;
            }
            else
            {
                rdbskip.Checked = true;
            }
            drwNumName = config.AppSettings.Settings["DrawingNumName"].Value;
            startNum = config.AppSettings.Settings["StartNum"].Value;
        }
        catch
        {
        }
        cboEditRules.SelectedIndex = int.Parse(Settings.Default["PropRename"].ToString());
        switch (int.Parse(Settings.Default["ProcessRange"].ToString()))
        {
            case 0:
                rdoAllProp.Checked = true;
                break;
            case 1:
                rdoList.Checked = true;
                break;
            case 2:
                rdoListExcept.Checked = true;
                break;
        }
        txtPropName.Text = Settings.Default["PropName"].ToString();
        chkWriteModel.Checked = (bool)Settings.Default["IsWriteModel"];
        chkWriteDrw.Checked = (bool)Settings.Default["IsWriteDraw"];
        switch (int.Parse(Settings.Default["PropProcessRange"].ToString()))
        {
            case 0:
                chkCurrentConfig.Checked = true;
                break;
            case 1:
                chkAllConfig.Checked = true;
                break;
            case 2:
                chkCustomize.Checked = true;
                break;
        }
    }

    /// <summary>
    /// //初始化添加属性
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
    {
        try
        {
            // 保存配置信息
            config.AppSettings.Settings["FramePropName"].Value = txtPropName.Text.Trim();

            // 保存转移属性列表
            for (int k = 0; k < dgvTransfer.RowCount; k++)
            {
                object propName2 = dgvTransfer.Rows[k].Cells[0].Value;
                if (propName2 != null && propName2.ToString().Trim().Length > 0)
                {
                    config.AppSettings.Settings["Transfer" + k].Value = propName2.ToString();
                }
                else
                {
                    config.AppSettings.Settings["Transfer" + k].Value = "";
                }
            }

            // 保存移除属性列表
            for (int j = 0; j < dgvRemoveList.RowCount; j++)
            {
                object propName = dgvRemoveList.Rows[j].Cells[0].Value;
                if (propName != null && propName.ToString().Trim().Length > 0)
                {
                    config.AppSettings.Settings["Remove" + j].Value = propName.ToString();
                }
                else
                {
                    config.AppSettings.Settings["Remove" + j].Value = "";
                }
            }

            // 保存新增属性列表
            for (int i = 0; i < dgvAddPropertyList.RowCount; i++)
            {
                object propertyName = dgvAddPropertyList.Rows[i].Cells[0].Value;
                object propertyValue = dgvAddPropertyList.Rows[i].Cells[1].Value;
                if (propertyName != null && propertyName.ToString().Trim().Length > 0)
                {
                    config.AppSettings.Settings["AddPropertyName" + i].Value = propertyName.ToString();
                }
                if (propertyValue != null && propertyValue.ToString().Trim().Length > 0)
                {
                    config.AppSettings.Settings["AddPropertyValue" + i].Value = propertyValue.ToString();
                }
            }

            // 保存图纸编号和起始编号
            config.AppSettings.Settings["DrawingNumName"].Value = drwNumName;
            config.AppSettings.Settings["StartNum"].Value = startNum;

            // 保存属性存在性选项
            config.AppSettings.Settings["PropExistence"].Value = (rdbcover.Checked ? "0" : "1");

            // 保存配置文件
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
        catch
        {
            // 异常处理
        }
    }
    /// <summary>
    /// 打开帮助视频
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void tsVideoHelp_Click(object sender, EventArgs e)
    {
        try
        {
            Process.Start("http://smart3d.maidiyun.com/#/videoDetail/427/3");
        }
        catch (Exception)
        {
            MessageBox.Show("您的电脑无法打开.mp4视频文件！", "提示");
        }
    }
    /// <summary>
    /// 打开帮助文档
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void tsPDFHelp_Click(object sender, EventArgs e)
    {
        try
        {
            Process.Start(Application.StartupPath + "\\help\\helpDoc.pdf");
        }
        catch (Exception)
        {
            MessageBox.Show("请安装PDF浏览器！", "提示");
        }
    }

    /// <summary>
    /// 添加文件
    /// </summary>
    /// <param name="files"></param> 文件路径
    public void AddSwFile(string[] files)
    {
        List<string> listFile = listSwFile.Select((SwFile swFile) => swFile.FullName).ToList(); //获取已选择文件
        listFile.AddRange(files); //添加新文件
        listFile = listFile.Distinct().ToList(); //去重
        listSwFile = listFile.Select((string file) => new SwFile(file)).ToList(); //转换为SwFile
    }

    /// <summary>
    /// 添加文件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnAddFile_Click(object sender, EventArgs e)
    {
        string[] files = ControlHelper.FilesSelectionBoxs("零件、装配体、图纸|*.SLD*|零件|*.SLDPRT|装配体|*.SLDASM|图纸|*.SLDDRW|模板文件|*.*dot");
        if (files != null)
        {
            AddSwFile(files);
            dataGridView.DataSource = null;
            dataGridView.DataSource = listSwFile;
        }
    }

    /// <summary>
    /// 添加文件夹
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnAddDir_Click(object sender, EventArgs e)
    {
        string dir = ControlHelper.OpenfolderDialog();
        if (dir != null)
        {
            List<string> files = new List<string>();
            ControlHelper.GetSubFile(dir, 0, ref files, "*SLD*");
            string[] files2 = files.Where((string file) => Path.GetFileName(file)[0] != '~').ToArray();
            AddSwFile(files2);
            dataGridView.DataSource = null;
            dataGridView.DataSource = listSwFile;
        }
    }

    /// <summary>
    /// //编辑属性
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnActionFile_Click(object sender, EventArgs e)
    {
        try
        {
            sw.swApp = ConnectSW.iSwApp;
            if (sw.swApp != null)
            {
                string file = sw.GetActiveFileName();
                List<string> filels = new List<string>();
                if (Path.GetExtension(file).ToUpper() != ".SLDASM")
                {
                    filels.Add(file);
                }
                else
                {
                    filels = filels.Concat(sw.GetRefFiles(file)).ToList();
                }
                AddSwFile(filels.ToArray());
                dataGridView.DataSource = null;
                dataGridView.DataSource = listSwFile;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "温馨提示");
        }
    }

    /// <summary>
    /// //编辑属性
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnAllOpenFile_Click(object sender, EventArgs e)
    {
        try
        {
            sw.swApp = ConnectSW.iSwApp;
            if (sw.swApp != null)
            {
                string[] files = sw.GetActiveAllFileName();
                AddSwFile(files);
                dataGridView.DataSource = null;
                dataGridView.DataSource = listSwFile;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "温馨提示");
        }
    }

    private void btnSelectedItem_Click(object sender, EventArgs e)
    {
        try
        {
            sw.swApp = ConnectSW.iSwApp;
            if (sw.swApp != null)
            {
                string[] files = sw.GetSelectedItem();
                if (files.Length == 0)
                {
                    MessageBox.Show("您未选择任何零件或子装配体", "温馨提示");
                    return;
                }
                AddSwFile(files);
                dataGridView.DataSource = null;
                dataGridView.DataSource = listSwFile;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "温馨提示");
        }
    }

    private void dataGridView_DragDrop(object sender, DragEventArgs e)
    {
        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files == null)
        {
            return;
        }
        List<string> listFile = new List<string>();
        string[] array = files;
        foreach (string file in array)
        {
            switch (Path.GetExtension(file).ToUpper())
            {
                case ".SLDPRT":
                case ".SLDASM":
                case ".SLDDRW":
                case ".PRTDOT":
                case ".DRWDOT":
                case ".ASMDOT":
                    listFile.Add(file);
                    break;
            }
        }
        AddSwFile(listFile.ToArray());
        dataGridView.DataSource = null;
        dataGridView.DataSource = listSwFile;
    }

    private void dataGridView_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void 移除选择项ToolStripMenuItem_Click(object sender, EventArgs e)
    {
        List<int> selectedRowIndex = new List<int>();
        foreach (DataGridViewRow row in dataGridView.SelectedRows)
        {
            selectedRowIndex.Add(row.Index);
        }
        foreach (int index in selectedRowIndex.OrderByDescending((int i) => i))
        {
            listSwFile.RemoveAt(index);
        }
        dataGridView.DataSource = null;
        dataGridView.DataSource = listSwFile;
        SuccessRendering();
    }

    private void 移除全部零件ToolStripMenuItem_Click(object sender, EventArgs e)
    {
        listSwFile.RemoveAll((SwFile swfile) => swfile.FileType == "零件");
        dataGridView.DataSource = null;
        dataGridView.DataSource = listSwFile;
        SuccessRendering();
    }

    private void 移除全部装配体ToolStripMenuItem_Click(object sender, EventArgs e)
    {
        listSwFile.RemoveAll((SwFile swfile) => swfile.FileType == "装配");
        dataGridView.DataSource = null;
        dataGridView.DataSource = listSwFile;
        SuccessRendering();
    }

    private void 移除全部图纸ToolStripMenuItem_Click(object sender, EventArgs e)
    {
        listSwFile.RemoveAll((SwFile swfile) => swfile.FileType == "图纸");
        dataGridView.DataSource = null;
        dataGridView.DataSource = listSwFile;
        SuccessRendering();
    }

    private void 移除全部模板文件ToolStripMenuItem_Click(object sender, EventArgs e)
    {
        listSwFile.RemoveAll((SwFile swfile) => swfile.FileType == "模板");
        dataGridView.DataSource = null;
        dataGridView.DataSource = listSwFile;
        SuccessRendering();
    }

    private void tsBtnClear_Click(object sender, EventArgs e)
    {
        if (listSwFile.Count != 0 && MessageBox.Show("请确认要清除全部已选择文件", "提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
        {
            dataGridView.DataSource = null;
            listSwFile = new List<SwFile>();
        }
    }

    private void SuccessRendering()
    {
        foreach (DataGridViewRow row in (IEnumerable)dataGridView.Rows)
        {
            if (row.Cells["EditResult"].Value.ToString() == "已修改")
            {
                row.DefaultCellStyle.BackColor = Color.YellowGreen;
            }
            else
            {
                row.DefaultCellStyle.BackColor = Color.White;
            }
        }
    }

    private void CboEditRules_SelectedIndexChanged(object sender, EventArgs e)
    {
        DataGridViewCellStyle input = new DataGridViewCellStyle();
        DataGridViewCellStyle readOnly = new DataGridViewCellStyle();
        readOnly.BackColor = SystemColors.ControlLight;
        dgvEditProp.Rows[0].Cells["Rule"].Value = cboEditRules.Text;
        switch (cboEditRules.Text)
        {
            case "原名 → 新值":
                dgvEditProp.Columns["OldName"].ReadOnly = false;
                dgvEditProp.Columns["OldName"].DefaultCellStyle = input;
                dgvEditProp.Columns["OldValue"].ReadOnly = true;
                dgvEditProp.Rows[0].Cells["OldValue"].Value = null;
                dgvEditProp.Columns["OldValue"].DefaultCellStyle = readOnly;
                dgvEditProp.Columns["NewName"].ReadOnly = true;
                dgvEditProp.Columns["NewName"].DefaultCellStyle = readOnly;
                dgvEditProp.Rows[0].Cells["NewName"].Value = null;
                cboNewValue.Visible = true;
                dgvEditProp.Columns["NewValue"].ReadOnly = false;
                dgvEditProp.Columns["NewValue"].DefaultCellStyle = input;
                break;
            case "原值 → 新值":
                dgvEditProp.Columns["OldName"].ReadOnly = true;
                dgvEditProp.Columns["OldName"].DefaultCellStyle = readOnly;
                dgvEditProp.Rows[0].Cells["OldName"].Value = null;
                dgvEditProp.Columns["OldValue"].ReadOnly = false;
                dgvEditProp.Columns["OldValue"].DefaultCellStyle = input;
                dgvEditProp.Columns["NewName"].ReadOnly = true;
                dgvEditProp.Columns["NewName"].DefaultCellStyle = readOnly;
                dgvEditProp.Rows[0].Cells["NewName"].Value = null;
                cboNewValue.Visible = true;
                dgvEditProp.Columns["NewValue"].ReadOnly = false;
                dgvEditProp.Columns["NewValue"].DefaultCellStyle = input;
                break;
            case "（原名、原值） → 新值":
                dgvEditProp.Columns["OldName"].ReadOnly = false;
                dgvEditProp.Columns["OldName"].DefaultCellStyle = input;
                dgvEditProp.Columns["OldValue"].ReadOnly = false;
                dgvEditProp.Columns["OldValue"].DefaultCellStyle = input;
                dgvEditProp.Columns["NewName"].ReadOnly = true;
                dgvEditProp.Columns["NewName"].DefaultCellStyle = readOnly;
                dgvEditProp.Rows[0].Cells["NewName"].Value = null;
                cboNewValue.Visible = true;
                dgvEditProp.Columns["NewValue"].ReadOnly = false;
                dgvEditProp.Columns["NewValue"].DefaultCellStyle = input;
                break;
            case "修改属性名":
                dgvEditProp.Columns["OldName"].ReadOnly = false;
                dgvEditProp.Columns["OldName"].DefaultCellStyle = input;
                dgvEditProp.Columns["OldValue"].ReadOnly = true;
                dgvEditProp.Columns["OldValue"].DefaultCellStyle = readOnly;
                dgvEditProp.Rows[0].Cells["OldValue"].Value = null;
                dgvEditProp.Columns["NewName"].ReadOnly = false;
                dgvEditProp.Columns["NewName"].DefaultCellStyle = input;
                cboNewValue.Visible = false;
                dgvEditProp.Columns["NewValue"].ReadOnly = true;
                dgvEditProp.Columns["NewValue"].DefaultCellStyle = readOnly;
                break;
            case "添加属性":
                dgvEditProp.Columns["OldName"].ReadOnly = true;
                dgvEditProp.Columns["OldName"].DefaultCellStyle = readOnly;
                dgvEditProp.Rows[0].Cells["OldName"].Value = null;
                dgvEditProp.Columns["OldValue"].ReadOnly = true;
                dgvEditProp.Columns["OldValue"].DefaultCellStyle = readOnly;
                dgvEditProp.Rows[0].Cells["OldValue"].Value = null;
                dgvEditProp.Columns["NewName"].ReadOnly = false;
                dgvEditProp.Columns["NewName"].DefaultCellStyle = input;
                cboNewValue.Visible = true;
                dgvEditProp.Columns["NewValue"].ReadOnly = false;
                dgvEditProp.Columns["NewValue"].DefaultCellStyle = input;
                break;
            case "关键字替换":
                dgvEditProp.Columns["OldName"].ReadOnly = true;
                dgvEditProp.Columns["OldName"].DefaultCellStyle = readOnly;
                dgvEditProp.Rows[0].Cells["OldName"].Value = null;
                dgvEditProp.Columns["OldValue"].ReadOnly = false;
                dgvEditProp.Columns["OldValue"].DefaultCellStyle = input;
                dgvEditProp.Columns["NewName"].ReadOnly = true;
                dgvEditProp.Columns["NewName"].DefaultCellStyle = readOnly;
                dgvEditProp.Rows[0].Cells["NewName"].Value = null;
                cboNewValue.Visible = true;
                dgvEditProp.Columns["NewValue"].ReadOnly = false;
                dgvEditProp.Columns["NewValue"].DefaultCellStyle = input;
                break;
            case "关键字替换2":
                dgvEditProp.Columns["OldName"].ReadOnly = false;
                dgvEditProp.Columns["OldName"].DefaultCellStyle = input;
                dgvEditProp.Columns["OldValue"].ReadOnly = false;
                dgvEditProp.Columns["OldValue"].DefaultCellStyle = input;
                dgvEditProp.Columns["NewName"].ReadOnly = true;
                dgvEditProp.Columns["NewName"].DefaultCellStyle = readOnly;
                dgvEditProp.Rows[0].Cells["NewName"].Value = null;//新名
                cboNewValue.Visible = true;
                dgvEditProp.Columns["NewValue"].ReadOnly = false;
                dgvEditProp.Columns["NewValue"].DefaultCellStyle = input;
                break;
            case "拷贝(新名 → 旧值)":
                dgvEditProp.Columns["OldName"].ReadOnly = false;
                dgvEditProp.Columns["OldName"].DefaultCellStyle = input;
                //dgvEditProp.Rows[0].Cells["OldName"].Value = null;
                dgvEditProp.Columns["OldValue"].ReadOnly = true;
                dgvEditProp.Columns["OldValue"].DefaultCellStyle = readOnly;
                dgvEditProp.Columns["NewName"].ReadOnly = false;
                dgvEditProp.Columns["NewName"].DefaultCellStyle = input;
                dgvEditProp.Rows[0].Cells["NewName"].Value = null;
                cboNewValue.Visible = false;
                dgvEditProp.Columns["NewValue"].ReadOnly = true;
                dgvEditProp.Columns["NewValue"].DefaultCellStyle = readOnly;
                break;
        }
        dgvEditProp.ClearSelection();
    }

    private void btnAddEditRules_Click(object sender, EventArgs e)
    {
        for (int i = 0; i < 4; i++)
        {
            if (!dgvEditProp.Columns[i].ReadOnly)
            {
                object value = ((i != 3) ? dgvEditProp.Rows[0].Cells[i].Value : cboNewValue.Text);
                if (value == null)
                {
                    string colText = dgvEditProp.Columns[i].HeaderText;
                    MessageBox.Show("列“" + colText + "”不能为空，请输入值或更换修改规则。", "提示");
                    return;
                }
            }
        }
        SwProperty swProp = (swProp = new SwProperty());
        switch (cboEditRules.Text)
        {
            case "修改属性名":
                swProp.Old_Name = dgvEditProp.Rows[0].Cells["OldName"].Value.ToString();
                swProp.New_Name = dgvEditProp.Rows[0].Cells["NewName"].Value.ToString();
                break;
            case "原名 → 新值":
                swProp.Old_Name = dgvEditProp.Rows[0].Cells["OldName"].Value.ToString();
                swProp.New_Value = cboNewValue.Text;
                break;
            case "（原名、原值） → 新值":
                swProp.Old_Name = dgvEditProp.Rows[0].Cells["OldName"].Value.ToString();
                swProp.Old_Value = dgvEditProp.Rows[0].Cells["OldValue"].Value.ToString();
                swProp.New_Value = cboNewValue.Text;
                break;
            case "原值 → 新值":
                swProp.Old_Value = dgvEditProp.Rows[0].Cells["OldValue"].Value.ToString();
                swProp.New_Value = cboNewValue.Text;
                break;
            case "添加属性":
                swProp.New_Name = dgvEditProp.Rows[0].Cells["NewName"].Value.ToString();
                swProp.New_Value = cboNewValue.Text;
                break;
            case "关键字替换":
                swProp.Old_Value = "{" + dgvEditProp.Rows[0].Cells["OldValue"].Value.ToString() + "}";
                swProp.New_Value = cboNewValue.Text;
                break;
            case "关键字替换2":
                swProp.Old_Name = dgvEditProp.Rows[0].Cells["OldName"].Value.ToString();
                swProp.Old_Value = "{" + dgvEditProp.Rows[0].Cells["OldValue"].Value.ToString() + "}";
                swProp.New_Value = cboNewValue.Text;
                break;
            case "拷贝(新名 → 旧值)":
                swProp.Old_Name = dgvEditProp.Rows[0].Cells["OldName"].Value.ToString();
                swProp.Old_Value = "{" + dgvEditProp.Rows[0].Cells["OldValue"].Value.ToString() + "}";
                swProp.New_Name = dgvEditProp.Rows[0].Cells["NewName"].Value.ToString();
                break;
        }
        if (swProp.Old_Name != null && !(swProp.EditType == "修改属性名") && listSwProp.Count((SwProperty item) => item.Old_Name == swProp.Old_Name) > 0)
        {
            MessageBox.Show("原名称“" + swProp.Old_Name + "”已经存在", "提示");
            return;
        }
        if (swProp.New_Name != null && listSwProp.Count((SwProperty item) => item.New_Name == swProp.New_Name) > 0)
        {
            MessageBox.Show("新名称“" + swProp.New_Name + "”已经存在", "提示");
            return;
        }
        listSwProp.Add(swProp);
        dgv_EditProp.DataSource = null;
        dgv_EditProp.DataSource = listSwProp;
    }

    private void btnStart_Click(object sender, EventArgs e)
    {
        if (thread != null && thread.IsAlive)
        {
            return;
        }
        if (listSwFile.Count == 0 || listSwFile == null)
        {
            MessageBox.Show("请选择文件", "提示");
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
        catch (Exception ex2)
        {
            MessageBox.Show(ex2.Message, "温馨提示");
            return;
        }
        try
        {
            List<SwProperty> listFailure = new List<SwProperty>();
            List<string> propNames = new List<string>();
            int type = 1;
            int index = 0;
            switch (tabControl.SelectedTab.Name)
            {
                case "tpEditProp": // 属性编辑
                    if (listSwProp.Count == 0 || listSwProp == null)
                    {
                        MessageBox.Show("请输入修改规则", "提示");
                        return;
                    }
                    thread = new Thread((ThreadStart)delegate
                    {
                        try
                        {
                            foreach (SwFile current2 in listSwFile)
                            {
                                dataGridView.Invoke((Action)delegate
                                {
                                    dataGridView.CurrentCell = dataGridView.Rows[index].Cells[0];
                                });
                                sw.OpenDoc(current2);
                                if (current2.FileType_Int == 3)
                                {
                                    sw.EditCustomInfoByFile(current2, listSwProp);
                                }
                                else
                                {
                                    if (chkCurrentConfig.Checked)
                                    {
                                        sw.EditCutProp(current2, listSwProp);
                                    }
                                    if (chkAllConfig.Checked)
                                    {
                                        sw.EditAllProp(current2, listSwProp);
                                    }
                                    if (chkCustomize.Checked)
                                    {
                                        sw.EditCustomInfoByFile(current2, listSwProp);
                                    }
                                }
                                sw.CloseDoc(isSave: true);
                                Invoke((Action)delegate
                                {
                                    SuccessRendering(index, listSwFile, listFailure);
                                });
                                index++;
                            }
                            List<SwProperty> list = new List<SwProperty>();
                            foreach (SwProperty current3 in listFailure)
                            {
                                if (current3.ErrorInfo != null)
                                {
                                    list.Add(current3);
                                }
                            }
                            listFailure = list;
                            if (listFailure.Count == 0)
                            {
                                MessageBox.Show("全部修改成功！", "提示");
                            }
                            else
                            {
                                Invoke((Action)delegate
                                {
                                    FrmFailToEdit frmFailToEdit = new FrmFailToEdit();
                                    frmFailToEdit.delegatCheckEditResult = CheckEditResult;
                                    frmFailToEdit.listFailure = listFailure;
                                    frmFailToEdit.Show();
                                });
                            }
                        }
                        catch (Exception ex4)
                        {
                            MessageBox.Show(ex4.Message, "温馨提示");
                        }
                    });
                    thread.IsBackground = false;
                    thread.Start();
                    break;
                case "tpPorpTransfer": // 属性转移
                    {
                        for (int j = 0; j < dgvTransfer.RowCount; j++)
                        {
                            object propName2 = dgvTransfer.Rows[j].Cells[0].Value;
                            if (propName2 != null && propName2.ToString().Trim().Length > 0)
                            {
                                propNames.Add(propName2.ToString());
                            }
                        }
                        if (rdoAllProp.Checked)
                        {
                            type = 1;
                        }
                        else if (rdoList.Checked)
                        {
                            type = 2;
                            if (propNames.Count == 0)
                            {
                                MessageBox.Show("请输入需要转移的属性名", "提示");
                            }
                        }
                        else
                        {
                            type = 3;
                        }
                        List<SwFile> swFiles = listSwFile.FindAll((SwFile file) => file.FileType_Int != 3);
                        if (swFiles.Count == 0)
                        {
                            MessageBox.Show("请选择非图纸文件。", "提示");
                            return;
                        }
                        thread = new Thread((ThreadStart)delegate
                        {
                            try
                            {
                                foreach (SwFile current4 in swFiles)
                                {
                                    index = listSwFile.IndexOf(current4);
                                    dataGridView.Invoke((Action)delegate
                                    {
                                        dataGridView.CurrentCell = dataGridView.Rows[index].Cells[0];
                                    });
                                    if (current4.FileType_Int != 3)
                                    {
                                        sw.OpenDoc(current4);
                                        if (lblDirection.Tag.ToString() == "右")
                                        {
                                            sw.CustomTransferToConfig(current4, type, propNames);
                                        }
                                        else
                                        {
                                            sw.ConfigTransferToCustom(current4, type, propNames);
                                        }
                                        sw.CloseDoc(isSave: true);
                                        Invoke((Action)delegate
                                        {
                                            SuccessRendering(index, listSwFile, listFailure);
                                        });
                                    }
                                }
                                if (listFailure.Count == 0)
                                {
                                    MessageBox.Show("全部修改成功！", "提示");
                                }
                                else
                                {
                                    Invoke((Action)delegate
                                    {
                                        FrmFailToTransfer frmFailToTransfer = new FrmFailToTransfer();
                                        frmFailToTransfer.delegatCheckEditResult = CheckEditResult;
                                        frmFailToTransfer.listFailure = listFailure;
                                        frmFailToTransfer.Show();
                                    });
                                }
                            }
                            catch (Exception ex5)
                            {
                                MessageBox.Show(ex5.Message, "温馨提示");
                            }
                        });
                        thread.IsBackground = false;
                        thread.Start();
                        break;
                    }
                case "tpPorpRemove": // 属性移除
                    {
                        for (int i = 0; i < dgvRemoveList.RowCount; i++)
                        {
                            object propName = dgvRemoveList.Rows[i].Cells[0].Value;
                            if (propName != null && propName.ToString().Trim().Length > 0)
                            {
                                propNames.Add(propName.ToString());
                            }
                        }
                        if (rdoAllProp.Checked)
                        {
                            type = 1;
                        }
                        else if (rdoList.Checked)
                        {
                            type = 2;
                            if (propNames.Count == 0)
                            {
                                MessageBox.Show("请输入需要转移的属性名", "提示");
                            }
                        }
                        else
                        {
                            type = 3;
                        }
                        thread = new Thread((ThreadStart)delegate
                        {
                            try
                            {
                                foreach (SwFile current in listSwFile)
                                {
                                    dataGridView.Invoke((Action)delegate
                                    {
                                        dataGridView.CurrentCell = dataGridView.Rows[index].Cells[0];
                                    });
                                    sw.OpenDoc(current);
                                    if (current.FileType_Int == 3)
                                    {
                                        sw.DelectCustomInfo(current, type, propNames);
                                    }
                                    else
                                    {
                                        if (chkCurrentConfig.Checked)
                                        {
                                            sw.DelectConfigProp(current, type, propNames);
                                        }
                                        if (chkAllConfig.Checked)
                                        {
                                            sw.DelectAllConfigProp(current, type, propNames);
                                        }
                                        if (chkCustomize.Checked)
                                        {
                                            sw.DelectCustomInfo(current, type, propNames);
                                        }
                                    }
                                    sw.CloseDoc(isSave: true);
                                    Invoke((Action)delegate
                                    {
                                        SuccessRendering(index, listSwFile, listFailure);
                                    });
                                    index++;
                                }
                                if (listFailure.Count == 0)
                                {
                                    MessageBox.Show("全部移除成功！", "提示");
                                }
                                else
                                {
                                    MessageBox.Show("部分移除失败！", "提示");
                                }
                            }
                            catch (Exception ex3)
                            {
                                MessageBox.Show(ex3.Message, "温馨提示");
                            }
                        });
                        thread.IsBackground = false;
                        thread.Start();
                        break;
                    }
                case "tpAddFrame": // 添加图框大小大属性中
                    if (!chkWriteModel.Checked && !chkWriteDrw.Checked)
                    {
                        MessageBox.Show("请选择要将图号写入模型或是图纸", "提示");
                        return;
                    }
                    if (listSwFile.Count((SwFile file) => file.FileType == "图纸") == 0)
                    {
                        MessageBox.Show("请选择图纸文件。", "提示");
                        return;
                    }
                    thread = new Thread((ThreadStart)delegate
                    {
                        try
                        {
                            foreach (SwFile current5 in listSwFile)
                            {
                                dataGridView.Invoke((Action)delegate
                                {
                                    dataGridView.CurrentCell = dataGridView.Rows[index].Cells[0];
                                });
                                if (current5.FileType == "图纸")
                                {
                                    sw.OpenDoc(current5);
                                    current5.listSwProperty = new List<SwProperty>();
                                    current5.listSwProperty.Add(new SwProperty
                                    {
                                        New_Name = txtPropName.Text.Trim()
                                    });
                                    sw.AddFrame(current5, chkCustomize.Checked, chkWriteModel.Checked, chkWriteDrw.Checked);
                                    sw.CloseDoc(isSave: true);
                                    Invoke((Action)delegate
                                    {
                                        SuccessRendering(index, listSwFile, listFailure);
                                    });
                                }
                                index++;
                            }
                            if (listFailure.Count == 0)
                            {
                                MessageBox.Show("全部添加成功！", "提示");
                            }
                        }
                        catch (Exception ex6)
                        {
                            MessageBox.Show(ex6.Message, "温馨提示");
                        }
                    });
                    thread.IsBackground = false;
                    thread.Start();
                    break;
                case "tbAddProperty":
                    {
                        List<SwAddProperty> swAddProperties = new List<SwAddProperty>();
                        thread = new Thread((ThreadStart)delegate
                        {
                            int skip = (rdbskip.Checked ? 1 : 0);
                            foreach (SwFile current6 in listSwFile)
                            {
                                current6.listSwAddProperty = listSwAddProperty;
                                sw.OpenDoc(current6);
                                if (chkCurrentConfig.Checked)
                                {
                                    sw.AddConfigProperty(current6, skip);
                                }
                                if (chkCustomize.Checked)
                                {
                                    sw.AddCustomProperty(current6, skip);
                                }
                                if (chkAllConfig.Checked)
                                {
                                    sw.AddAllConfigProperty(current6, skip);
                                }
                                sw.CloseDoc(isSave: true);
                                Invoke((Action)delegate
                                {
                                    SuccessAddProperty(index, listSwFile, swAddProperties);
                                });
                                int num = index;
                                index = num + 1;
                            }
                            if (swAddProperties.Count == 0)
                            {
                                MessageBox.Show("全部添加属性成功", "提示");
                            }
                            else
                            {
                                MessageBox.Show("部分添加属性成功", "提示");
                            }
                        });
                        thread.IsBackground = false;
                        thread.Start();
                        break;
                    }
            }
            SavaSetting();
        }
        catch (Exception ex)
        {
            MessageBox.Show("运行出错：" + ex.Message, "异常提示");
        }
    }

    private void SavaSetting()
    {
        int propRename = cboEditRules.Items.IndexOf(cboEditRules.SelectedItem);
        int processRange = ((!rdoAllProp.Checked) ? (rdoList.Checked ? 1 : 2) : 0);
        string propName = txtPropName.Text;
        bool isWriteModel = chkWriteModel.Checked;
        bool isWriteDraw = chkWriteDrw.Checked;
        Settings.Default["PropRename"] = propRename;
        Settings.Default["ProcessRange"] = processRange;
        Settings.Default["PropName"] = propName;
        Settings.Default["IsWriteModel"] = isWriteModel;
        Settings.Default["IsWriteDraw"] = isWriteDraw;
        int propProcessRange = ((!chkCurrentConfig.Checked) ? (chkAllConfig.Checked ? 1 : 2) : 0);
        Settings.Default["PropProcessRange"] = propProcessRange;
        Settings.Default.Save();
    }

    private void SuccessAddProperty(int index, List<SwFile> swFiles, List<SwAddProperty> listSwAddProperty)
    {
        SwFile swFile = swFiles[index];
        string[] results = new string[3] { "添加成功", "已跳过", "覆盖成功" };
        List<SwAddProperty> props = swFile.listSwAddProperty.FindAll((SwAddProperty prop) => !results.Contains(prop.AddResult));
        if (props.Count() == 0)
        {
            swFile.EditResult = "已添加";
            dataGridView.Rows[index].DefaultCellStyle.BackColor = Color.YellowGreen;
        }
        else if (props.Count() < swFile.listSwProperty.Count)
        {
            swFile.EditResult = "部分添加";
            dataGridView.Rows[index].DefaultCellStyle.BackColor = Color.Yellow;
            listSwAddProperty.AddRange(props);
        }
        else
        {
            swFile.EditResult = "未添加";
            dataGridView.Rows[index].DefaultCellStyle.BackColor = Color.White;
            listSwAddProperty.AddRange(props);
        }
        dataGridView.Refresh();
    }

    private void SuccessRendering(int index, List<SwFile> swFiles, List<SwProperty> listFailure)
    {
        SwFile swFile = swFiles[index];
        List<SwProperty> props = swFile.listSwProperty.FindAll((SwProperty prop) => prop.ErrorInfo != "修改成功");
        if (props.Count() == 0)
        {
            swFile.EditResult = "已修改";
            dataGridView.Rows[index].DefaultCellStyle.BackColor = Color.YellowGreen;
        }
        else if (props.Count() < swFile.listSwProperty.Count)
        {
            swFile.EditResult = "部分修改";
            dataGridView.Rows[index].DefaultCellStyle.BackColor = Color.Yellow;
            listFailure.AddRange(props);
        }
        else
        {
            swFile.EditResult = "未修改";
            dataGridView.Rows[index].DefaultCellStyle.BackColor = Color.White;
            listFailure.AddRange(props);
        }
        dataGridView.Refresh();
    }

    private void dgv_EditProp_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right && e.ColumnIndex > -1 && e.RowIndex > -1)
        {
            dgv_EditProp.ClearSelection();
            dgv_EditProp.Rows[e.RowIndex].Selected = true;
            ControlHelper.NewContextMenuStrip("删除行", TsmDeleteRow_MouseUp).Show(Control.MousePosition.X, Control.MousePosition.Y);
        }
    }

    private void TsmDeleteRow_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            int selectedRowIndex = dgv_EditProp.SelectedRows[0].Index;
            listSwProp.RemoveAt(selectedRowIndex);
            dgv_EditProp.DataSource = null;
            dgv_EditProp.DataSource = listSwProp;
        }
    }

    private void CheckEditResult(string fileName)
    {
        SwFile swFile = listSwFile.Find((SwFile file) => file.FullName == fileName);
        if (swFile == null)
        {
            return;
        }
        int index = listSwFile.IndexOf(swFile);
        if (swFile.listSwProperty.Count > 0)
        {
            List<SwProperty> props = swFile.listSwProperty.FindAll((SwProperty prop) => prop.ErrorInfo != "修改成功");
            if (props.Count() == 0)
            {
                swFile.EditResult = "已修改";
                dataGridView.Rows[index].DefaultCellStyle.BackColor = Color.YellowGreen;
            }
            else if (props.Count() < swFile.listSwProperty.Count)
            {
                swFile.EditResult = "部分修改";
                dataGridView.Rows[index].DefaultCellStyle.BackColor = Color.Yellow;
            }
            else
            {
                swFile.EditResult = "未修改";
                dataGridView.Rows[index].DefaultCellStyle.BackColor = Color.White;
            }
        }
        dataGridView.Refresh();
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
                sw.OpenDoc(listSwFile[index]);
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
            Process.Start(Path.GetDirectoryName(listSwFile[index].FullName));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "温馨提示");
        }
    }

    private void lblDirection_Click(object sender, EventArgs e)
    {
        if (lblDirection.Tag.ToString() == "左")
        {
            lblDirection.Image = Resources.right_32px;
            lblDirection.Tag = "右";
        }
        else
        {
            lblDirection.Image = Resources.left_32px;
            lblDirection.Tag = "左";
        }
    }

    private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
    {
        switch (tabControl.SelectedTab.Name)
        {
            case "tpEditProp":
                pnlType.Visible = false;
                label2.Visible = true;
                break;
            case "tpPorpTransfer":
                pnlType.Visible = true;
                label2.Visible = true;
                break;
            case "tpPorpRemove":
                pnlType.Visible = true;
                label2.Visible = true;
                break;
            case "tpAddFrame":
                pnlType.Visible = false;
                label2.Visible = true;
                break;
            case "tbAddProperty":
                pnlType.Visible = false;
                label2.Visible = false;
                break;
        }
    }

    private void btnAddDrwNum_Click(object sender, EventArgs e)
    {
        List<SwFile> listSwDrw = listSwFile.Where((SwFile drw) => drw.FileType == "图纸").ToList();
        FrmAddDrwNum objForm = new FrmAddDrwNum();
        objForm.SetConfigDelegate = SetFrmAddDrwNumConfig;
        objForm.drwNumName = drwNumName;
        objForm.startNum = startNum;
        if (listSwDrw.Count > 0)
        {
            objForm.listSwDrw = listSwDrw;
        }
        else
        {
            objForm.listSwDrw = new List<SwFile>();
        }
        objForm.Show();
    }

    public void SetFrmAddDrwNumConfig(string objDrwNumName, string objStartNum)
    {
        drwNumName = objDrwNumName;
        startNum = objStartNum;
    }

    private void FrmMain_Load(object sender, EventArgs e)
    {
    }

    private void chkCurrentConfig_CheckedChanged(object sender, EventArgs e)
    {
        if (chkCurrentConfig.Checked && chkAllConfig.Checked)
        {
            chkAllConfig.Checked = false;
        }
    }

    private void chkAllConfig_CheckedChanged(object sender, EventArgs e)
    {
        if (chkAllConfig.Checked && chkCurrentConfig.Checked)
        {
            chkCurrentConfig.Checked = false;
        }
    }

    private void btnAddAsmFile_Click(object sender, EventArgs e)
    {
        string[] asmfile = ControlHelper.FilesSelectionBoxs("装配体|*.SLDASM");
        if (asmfile != null)
        {
            List<string> filels = new List<string>();
            string[] array = asmfile;
            foreach (string item in array)
            {
                filels = filels.Concat(sw.GetRefFiles(item)).ToList();
            }
            AddSwFile(filels.ToArray());
            dataGridView.DataSource = null;
            dataGridView.DataSource = listSwFile;
        }
    }

    private void InitializeAddProperty()
    {
        cmbPropertyValue.Items.Add("$文件名中的代号");
        cmbPropertyValue.Items.Add("$文件名中的名称");
        cmbPropertyValue.Items.Add("$文件名");
        cmbPropertyValue.Items.Add("$文件名[-][1]");
        cmbPropertyValue.Items.Add("$文件名[-][2]");
        cmbPropertyValue.Items.Add("$文件名[_][1]");
        cmbPropertyValue.Items.Add("$文件名[_][2]");
        cmbPropertyValue.Items.Add("$文件名[_][]");
        cmbPropertyValue.Items.Add("$密度");
        cmbPropertyValue.Items.Add("$材料");
        cmbPropertyValue.Items.Add("$体积");
        cmbPropertyValue.Items.Add("$表面积");
        cmbPropertyValue.Items.Add("$配置名");
        cmbPropertyValue.Items.Add("$短日期");
    }

    private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        new FrmExplain().Show();
    }

    private void btnAddlist_Click(object sender, EventArgs e)
    {
        for (int i = 0; i < 2; i++)
        {
            if (!dgvAddProperty.Columns[i].ReadOnly)
            {
                object value = ((i != 1) ? dgvAddProperty.Rows[0].Cells[i].Value : cmbPropertyValue.Text);
                if (value == null)
                {
                    string colText = dgvAddProperty.Columns[i].HeaderText;
                    MessageBox.Show("列“" + colText + "”不能为空，请输入属性名称或更换修改规则。", "提示");
                    return;
                }
            }
        }
        string propertyName = dgvAddProperty.Rows[0].Cells[0].Value.ToString();
        string rule = cmbPropertyValue.Text;
        if (listSwAddProperty.Count((SwAddProperty f) => f.PropertyName == propertyName) > 0)
        {
            MessageBox.Show("属性名“" + propertyName + "”已经存在", "提示");
            return;
        }
        listSwAddProperty.Add(new SwAddProperty(propertyName, rule));
        if (listSwAddProperty.Count > 0)
        {
            dgvAddPropertyList.DataSource = null;
            dgvAddPropertyList.DataSource = listSwAddProperty;
        }
    }

    private void dgvAddPropertyList_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right && e.ColumnIndex > -1 && e.RowIndex > -1)
        {
            dgvAddPropertyList.ClearSelection();
            dgvAddPropertyList.Rows[e.RowIndex].Selected = true;
            ControlHelper.NewContextMenuStrip("删除行", AddPropertyDeleteRow_MouseUp).Show(Control.MousePosition.X, Control.MousePosition.Y);
        }
    }

    private void AddPropertyDeleteRow_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            int selectedRowIndex = dgvAddPropertyList.SelectedRows[0].Index;
            listSwAddProperty.RemoveAt(selectedRowIndex);
            config.AppSettings.Settings["AddPropertyName" + selectedRowIndex].Value = "";
            config.AppSettings.Settings["AddPropertyValue" + selectedRowIndex].Value = "";
            dgvAddPropertyList.DataSource = null;
            dgvAddPropertyList.DataSource = listSwAddProperty;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
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
        this.components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PropertyEditingTool.FrmMain));
        this.menuStrip = new System.Windows.Forms.MenuStrip();
        this.toolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this.btnAddFile = new System.Windows.Forms.ToolStripMenuItem();
        this.btnAddAsmFile = new System.Windows.Forms.ToolStripMenuItem();
        this.btnAddDir = new System.Windows.Forms.ToolStripMenuItem();
        this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
        this.btnActionFile = new System.Windows.Forms.ToolStripMenuItem();
        this.btnAllOpenFile = new System.Windows.Forms.ToolStripMenuItem();
        this.btnSelectedItem = new System.Windows.Forms.ToolStripMenuItem();
        this.tsBtnReduce = new System.Windows.Forms.ToolStripMenuItem();
        this.移除选择项ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this.移除全部零件ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this.移除全部装配体ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this.移除全部图纸ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this.移除全部模板文件ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this.tsBtnClear = new System.Windows.Forms.ToolStripMenuItem();
        this.tsVideoHelp = new System.Windows.Forms.ToolStripMenuItem();
        this.tsPDFHelp = new System.Windows.Forms.ToolStripMenuItem();
        this.dataGridView = new System.Windows.Forms.DataGridView();
        this.modelName = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.FileType = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.EditResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.toolTip = new System.Windows.Forms.ToolTip(this.components);
        this.lblDirection = new System.Windows.Forms.Label();
        this.groupBox = new System.Windows.Forms.GroupBox();
        this.chkAllConfig = new System.Windows.Forms.CheckBox();
        this.chkCurrentConfig = new System.Windows.Forms.CheckBox();
        this.chkCustomize = new System.Windows.Forms.CheckBox();
        this.label2 = new System.Windows.Forms.Label();
        this.btnStart = new System.Windows.Forms.Button();
        this.tpAddFrame = new System.Windows.Forms.TabPage();
        this.chkWriteDrw = new System.Windows.Forms.CheckBox();
        this.chkWriteModel = new System.Windows.Forms.CheckBox();
        this.label3 = new System.Windows.Forms.Label();
        this.txtPropName = new System.Windows.Forms.TextBox();
        this.label1 = new System.Windows.Forms.Label();
        this.tpPorpRemove = new System.Windows.Forms.TabPage();
        this.dgvRemoveList = new System.Windows.Forms.DataGridView();
        this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.tpPorpTransfer = new System.Windows.Forms.TabPage();
        this.tpSeriesEdit = new System.Windows.Forms.TabPage(); // 系列编辑
        // TODO：
        this.label12 = new System.Windows.Forms.Label();
        this.label11 = new System.Windows.Forms.Label();
        this.label10 = new System.Windows.Forms.Label();
        this.dgvTransfer = new System.Windows.Forms.DataGridView();
        this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.pnlType = new System.Windows.Forms.Panel();
        this.label9 = new System.Windows.Forms.Label();
        this.rdoAllProp = new System.Windows.Forms.RadioButton();
        this.rdoList = new System.Windows.Forms.RadioButton();
        this.rdoListExcept = new System.Windows.Forms.RadioButton();
        this.tpEditProp = new System.Windows.Forms.TabPage();
        this.label7 = new System.Windows.Forms.Label();
        this.cboNewValue = new System.Windows.Forms.ComboBox();
        this.label6 = new System.Windows.Forms.Label();
        this.btnAddEditRules = new System.Windows.Forms.Button();
        this.label5 = new System.Windows.Forms.Label();
        this.cboEditRules = new System.Windows.Forms.ComboBox();
        this.dgvEditProp = new System.Windows.Forms.DataGridView();
        this.OldName = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.OldValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.NewName = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.NewValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.Rule = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.dgv_EditProp = new System.Windows.Forms.DataGridView();
        this.Old_Name = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.Old_Value = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.New_Name = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.New_Value = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.EditType = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.label4 = new System.Windows.Forms.Label();
        this.tabControl = new System.Windows.Forms.TabControl();
        this.tbAddProperty = new System.Windows.Forms.TabPage();
        this.dgvAddPropertyList = new System.Windows.Forms.DataGridView();
        this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.label13 = new System.Windows.Forms.Label();
        this.btnAddlist = new System.Windows.Forms.Button();
        this.cmbPropertyValue = new System.Windows.Forms.ComboBox();
        this.linkLabel1 = new System.Windows.Forms.LinkLabel();
        this.rdbcover = new System.Windows.Forms.RadioButton();
        this.rdbskip = new System.Windows.Forms.RadioButton();
        this.label8 = new System.Windows.Forms.Label();
        this.dgvAddProperty = new System.Windows.Forms.DataGridView();
        this.propertyName = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.propertyValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.btnAddDrwNum = new System.Windows.Forms.Button();
        this.menuStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)this.dataGridView).BeginInit();
        this.groupBox.SuspendLayout();
        this.tpAddFrame.SuspendLayout();
        this.tpPorpRemove.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)this.dgvRemoveList).BeginInit();
        this.tpPorpTransfer.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)this.dgvTransfer).BeginInit();
        this.pnlType.SuspendLayout();
        this.tpEditProp.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)this.dgvEditProp).BeginInit();
        ((System.ComponentModel.ISupportInitialize)this.dgv_EditProp).BeginInit();
        this.tabControl.SuspendLayout();
        this.tbAddProperty.SuspendLayout(); // 添加属性中
        ((System.ComponentModel.ISupportInitialize)this.dgvAddPropertyList).BeginInit();
        ((System.ComponentModel.ISupportInitialize)this.dgvAddProperty).BeginInit();
        base.SuspendLayout();
        this.menuStrip.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
        this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[5] { this.toolStripMenuItem, this.tsBtnReduce, this.tsBtnClear, this.tsVideoHelp, this.tsPDFHelp });
        this.menuStrip.Location = new System.Drawing.Point(0, 0);
        this.menuStrip.Name = "menuStrip";
        this.menuStrip.Padding = new System.Windows.Forms.Padding(8, 4, 0, 4);
        this.menuStrip.Size = new System.Drawing.Size(1088, 32);
        this.menuStrip.TabIndex = 0;
        this.menuStrip.Text = "menuStrip1";
        this.toolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[7] { this.btnAddFile, this.btnAddAsmFile, this.btnAddDir, this.toolStripSeparator1, this.btnActionFile, this.btnAllOpenFile, this.btnSelectedItem });
        this.toolStripMenuItem.Image = PropertyEditingTool.Properties.Resources.sw;
        this.toolStripMenuItem.Name = "toolStripMenuItem";
        this.toolStripMenuItem.Size = new System.Drawing.Size(93, 24);
        this.toolStripMenuItem.Text = "添加文件";
        this.btnAddFile.Image = (System.Drawing.Image)resources.GetObject("btnAddFile.Image");
        this.btnAddFile.Name = "btnAddFile";
        this.btnAddFile.Size = new System.Drawing.Size(242, 24);
        this.btnAddFile.Text = "添加文件";
        this.btnAddFile.Click += new System.EventHandler(btnAddFile_Click);
        this.btnAddAsmFile.Image = PropertyEditingTool.Properties.Resources.asm;
        this.btnAddAsmFile.Name = "btnAddAsmFile";
        this.btnAddAsmFile.Size = new System.Drawing.Size(242, 24);
        this.btnAddAsmFile.Text = "添加装配体(包括引用文件)";
        this.btnAddAsmFile.Click += new System.EventHandler(btnAddAsmFile_Click);
        this.btnAddDir.Image = PropertyEditingTool.Properties.Resources.folder_24;
        this.btnAddDir.Name = "btnAddDir";
        this.btnAddDir.Size = new System.Drawing.Size(242, 24);
        this.btnAddDir.Text = "添加文件夹";
        this.btnAddDir.Click += new System.EventHandler(btnAddDir_Click);
        this.toolStripSeparator1.Name = "toolStripSeparator1";
        this.toolStripSeparator1.Size = new System.Drawing.Size(239, 6);
        this.btnActionFile.Image = (System.Drawing.Image)resources.GetObject("btnActionFile.Image");
        this.btnActionFile.Name = "btnActionFile";
        this.btnActionFile.Size = new System.Drawing.Size(242, 24);
        this.btnActionFile.Text = "当前sw会话文件";
        this.btnActionFile.Click += new System.EventHandler(btnActionFile_Click);
        this.btnAllOpenFile.Image = PropertyEditingTool.Properties.Resources.prt;
        this.btnAllOpenFile.Name = "btnAllOpenFile";
        this.btnAllOpenFile.Size = new System.Drawing.Size(242, 24);
        this.btnAllOpenFile.Text = "当前sw打开的全部文件";
        this.btnAllOpenFile.Click += new System.EventHandler(btnAllOpenFile_Click);
        this.btnSelectedItem.Image = (System.Drawing.Image)resources.GetObject("btnSelectedItem.Image");
        this.btnSelectedItem.Name = "btnSelectedItem";
        this.btnSelectedItem.Size = new System.Drawing.Size(242, 24);
        this.btnSelectedItem.Text = "装配体选中项目";
        this.btnSelectedItem.Click += new System.EventHandler(btnSelectedItem_Click);
        this.tsBtnReduce.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[5] { this.移除选择项ToolStripMenuItem, this.移除全部零件ToolStripMenuItem, this.移除全部装配体ToolStripMenuItem, this.移除全部图纸ToolStripMenuItem, this.移除全部模板文件ToolStripMenuItem });
        this.tsBtnReduce.Image = (System.Drawing.Image)resources.GetObject("tsBtnReduce.Image");
        this.tsBtnReduce.Name = "tsBtnReduce";
        this.tsBtnReduce.Size = new System.Drawing.Size(79, 24);
        this.tsBtnReduce.Text = "移除项";
        this.移除选择项ToolStripMenuItem.Image = PropertyEditingTool.Properties.Resources.Document_Remove_24px;
        this.移除选择项ToolStripMenuItem.Name = "移除选择项ToolStripMenuItem";
        this.移除选择项ToolStripMenuItem.Size = new System.Drawing.Size(190, 24);
        this.移除选择项ToolStripMenuItem.Text = "移除选择项";
        this.移除选择项ToolStripMenuItem.Click += new System.EventHandler(移除选择项ToolStripMenuItem_Click);
        this.移除全部零件ToolStripMenuItem.Image = PropertyEditingTool.Properties.Resources.prt;
        this.移除全部零件ToolStripMenuItem.Name = "移除全部零件ToolStripMenuItem";
        this.移除全部零件ToolStripMenuItem.Size = new System.Drawing.Size(190, 24);
        this.移除全部零件ToolStripMenuItem.Text = "移除全部零件";
        this.移除全部零件ToolStripMenuItem.Click += new System.EventHandler(移除全部零件ToolStripMenuItem_Click);
        this.移除全部装配体ToolStripMenuItem.Image = PropertyEditingTool.Properties.Resources.asm;
        this.移除全部装配体ToolStripMenuItem.Name = "移除全部装配体ToolStripMenuItem";
        this.移除全部装配体ToolStripMenuItem.Size = new System.Drawing.Size(190, 24);
        this.移除全部装配体ToolStripMenuItem.Text = "移除全部装配体";
        this.移除全部装配体ToolStripMenuItem.Click += new System.EventHandler(移除全部装配体ToolStripMenuItem_Click);
        this.移除全部图纸ToolStripMenuItem.Image = PropertyEditingTool.Properties.Resources.dwg;
        this.移除全部图纸ToolStripMenuItem.Name = "移除全部图纸ToolStripMenuItem";
        this.移除全部图纸ToolStripMenuItem.Size = new System.Drawing.Size(190, 24);
        this.移除全部图纸ToolStripMenuItem.Text = "移除全部图纸";
        this.移除全部图纸ToolStripMenuItem.Click += new System.EventHandler(移除全部图纸ToolStripMenuItem_Click);
        this.移除全部模板文件ToolStripMenuItem.Image = PropertyEditingTool.Properties.Resources.file;
        this.移除全部模板文件ToolStripMenuItem.Name = "移除全部模板文件ToolStripMenuItem";
        this.移除全部模板文件ToolStripMenuItem.Size = new System.Drawing.Size(190, 24);
        this.移除全部模板文件ToolStripMenuItem.Text = "移除全部模板文件";
        this.移除全部模板文件ToolStripMenuItem.Click += new System.EventHandler(移除全部模板文件ToolStripMenuItem_Click);
        this.tsBtnClear.Image = (System.Drawing.Image)resources.GetObject("tsBtnClear.Image");
        this.tsBtnClear.Name = "tsBtnClear";
        this.tsBtnClear.Size = new System.Drawing.Size(93, 24);
        this.tsBtnClear.Text = "清除全部";
        this.tsBtnClear.Click += new System.EventHandler(tsBtnClear_Click);
        this.tsVideoHelp.Image = PropertyEditingTool.Properties.Resources.Video_32px;
        this.tsVideoHelp.Name = "tsVideoHelp";
        this.tsVideoHelp.Size = new System.Drawing.Size(93, 24);
        this.tsVideoHelp.Text = "视频演示";
        this.tsVideoHelp.Click += new System.EventHandler(tsVideoHelp_Click);
        this.tsPDFHelp.Image = PropertyEditingTool.Properties.Resources.document_pdf_32px_2;
        this.tsPDFHelp.Name = "tsPDFHelp";
        this.tsPDFHelp.Size = new System.Drawing.Size(120, 24);
        this.tsPDFHelp.Text = "PDF帮助文档";
        this.tsPDFHelp.Click += new System.EventHandler(tsPDFHelp_Click);
        this.dataGridView.AllowDrop = true;
        this.dataGridView.AllowUserToResizeColumns = false;
        this.dataGridView.AllowUserToResizeRows = false;
        this.dataGridView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.dataGridView.BackgroundColor = System.Drawing.Color.FromArgb(239, 255, 255);
        this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dataGridView.Columns.AddRange(this.modelName, this.FileType, this.EditResult);
        this.dataGridView.Location = new System.Drawing.Point(3, 32);
        this.dataGridView.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
        this.dataGridView.Name = "dataGridView";
        this.dataGridView.RowHeadersVisible = false;
        this.dataGridView.RowTemplate.Height = 23;
        this.dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.dataGridView.Size = new System.Drawing.Size(1081, 254);
        this.dataGridView.TabIndex = 1;
        this.dataGridView.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(dataGridView_CellMouseClick);
        this.dataGridView.DragDrop += new System.Windows.Forms.DragEventHandler(dataGridView_DragDrop);
        this.dataGridView.DragEnter += new System.Windows.Forms.DragEventHandler(dataGridView_DragEnter);
        this.modelName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        this.modelName.DataPropertyName = "Name";
        this.modelName.HeaderText = "文件名称";
        this.modelName.Name = "modelName";
        this.modelName.ReadOnly = true;
        this.modelName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
        this.FileType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
        this.FileType.DataPropertyName = "FileType";
        this.FileType.FillWeight = 150f;
        this.FileType.HeaderText = "类型";
        this.FileType.Name = "FileType";
        this.FileType.ReadOnly = true;
        this.FileType.Width = 62;
        this.EditResult.DataPropertyName = "EditResult";
        this.EditResult.HeaderText = "结果";
        this.EditResult.Name = "EditResult";
        this.EditResult.ReadOnly = true;
        this.EditResult.Width = 80;
        this.lblDirection.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        this.lblDirection.BackColor = System.Drawing.Color.FromArgb(239, 255, 255);
        this.lblDirection.Cursor = System.Windows.Forms.Cursors.Hand;
        this.lblDirection.Font = new System.Drawing.Font("微软雅黑", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
        this.lblDirection.Image = PropertyEditingTool.Properties.Resources.right_32px;
        this.lblDirection.Location = new System.Drawing.Point(733, 222);
        this.lblDirection.Name = "lblDirection";
        this.lblDirection.Size = new System.Drawing.Size(32, 32);
        this.lblDirection.TabIndex = 56;
        this.lblDirection.Tag = "右";
        this.lblDirection.Text = "     ";
        this.toolTip.SetToolTip(this.lblDirection, "点击可以更改转移方向");
        this.lblDirection.Click += new System.EventHandler(lblDirection_Click);
        this.groupBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        this.groupBox.Controls.Add(this.chkAllConfig);
        this.groupBox.Controls.Add(this.chkCurrentConfig);
        this.groupBox.Controls.Add(this.chkCustomize);
        this.groupBox.Controls.Add(this.label2);
        this.groupBox.Controls.Add(this.btnStart);
        this.groupBox.Location = new System.Drawing.Point(943, 299);
        this.groupBox.Name = "groupBox";
        this.groupBox.Size = new System.Drawing.Size(140, 288);
        this.groupBox.TabIndex = 3;
        this.groupBox.TabStop = false;
        this.groupBox.Text = "属性处理范围";
        this.chkAllConfig.AutoSize = true;
        this.chkAllConfig.Location = new System.Drawing.Point(17, 167);
        this.chkAllConfig.Name = "chkAllConfig";
        this.chkAllConfig.Size = new System.Drawing.Size(112, 24);
        this.chkAllConfig.TabIndex = 65;
        this.chkAllConfig.Text = "所有配置属性";
        this.chkAllConfig.UseVisualStyleBackColor = true;
        this.chkAllConfig.CheckedChanged += new System.EventHandler(chkAllConfig_CheckedChanged);
        this.chkCurrentConfig.AutoSize = true;
        this.chkCurrentConfig.Checked = true;
        this.chkCurrentConfig.CheckState = System.Windows.Forms.CheckState.Checked;
        this.chkCurrentConfig.Location = new System.Drawing.Point(17, 136);
        this.chkCurrentConfig.Name = "chkCurrentConfig";
        this.chkCurrentConfig.Size = new System.Drawing.Size(112, 24);
        this.chkCurrentConfig.TabIndex = 64;
        this.chkCurrentConfig.Text = "当前配置属性";
        this.chkCurrentConfig.UseVisualStyleBackColor = true;
        this.chkCurrentConfig.CheckedChanged += new System.EventHandler(chkCurrentConfig_CheckedChanged);
        this.chkCustomize.AutoSize = true;
        this.chkCustomize.Location = new System.Drawing.Point(17, 197);
        this.chkCustomize.Name = "chkCustomize";
        this.chkCustomize.Size = new System.Drawing.Size(98, 24);
        this.chkCustomize.TabIndex = 63;
        this.chkCustomize.Text = "自定义属性";
        this.chkCustomize.UseVisualStyleBackColor = true;
        this.label2.ForeColor = System.Drawing.SystemColors.Highlight;
        this.label2.Location = new System.Drawing.Point(6, 36);
        this.label2.Name = "label2";
        this.label2.Size = new System.Drawing.Size(125, 53);
        this.label2.TabIndex = 47;
        this.label2.Text = "注：图纸自动选择\r\n自定义属性";
        this.btnStart.Location = new System.Drawing.Point(10, 246);
        this.btnStart.Name = "btnStart";
        this.btnStart.Size = new System.Drawing.Size(123, 32);
        this.btnStart.TabIndex = 0;
        this.btnStart.Text = "执行";
        this.btnStart.UseVisualStyleBackColor = true;
        this.btnStart.Click += new System.EventHandler(btnStart_Click);
        this.tpAddFrame.BackColor = System.Drawing.Color.FromArgb(239, 255, 255);
        this.tpAddFrame.Controls.Add(this.chkWriteDrw);
        this.tpAddFrame.Controls.Add(this.chkWriteModel);
        this.tpAddFrame.Controls.Add(this.label3);
        this.tpAddFrame.Controls.Add(this.txtPropName);
        this.tpAddFrame.Controls.Add(this.label1);
        this.tpAddFrame.Location = new System.Drawing.Point(4, 29);
        this.tpAddFrame.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this.tpAddFrame.Name = "tpAddFrame";
        this.tpAddFrame.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this.tpAddFrame.Size = new System.Drawing.Size(926, 266);
        this.tpAddFrame.TabIndex = 6;
        this.tpAddFrame.Text = "添加图幅";
        this.chkWriteDrw.AutoSize = true;
        this.chkWriteDrw.Location = new System.Drawing.Point(34, 115);
        this.chkWriteDrw.Name = "chkWriteDrw";
        this.chkWriteDrw.Size = new System.Drawing.Size(98, 24);
        this.chkWriteDrw.TabIndex = 62;
        this.chkWriteDrw.Text = "写入工程图";
        this.chkWriteDrw.UseVisualStyleBackColor = true;
        this.chkWriteModel.AutoSize = true;
        this.chkWriteModel.Checked = true;
        this.chkWriteModel.CheckState = System.Windows.Forms.CheckState.Checked;
        this.chkWriteModel.Location = new System.Drawing.Point(34, 73);
        this.chkWriteModel.Name = "chkWriteModel";
        this.chkWriteModel.Size = new System.Drawing.Size(112, 24);
        this.chkWriteModel.TabIndex = 61;
        this.chkWriteModel.Text = "写入对应模型";
        this.chkWriteModel.UseVisualStyleBackColor = true;
        this.label3.AutoSize = true;
        this.label3.Location = new System.Drawing.Point(280, 28);
        this.label3.Name = "label3";
        this.label3.Size = new System.Drawing.Size(177, 20);
        this.label3.TabIndex = 54;
        this.label3.Text = "注：该操作仅针对图纸文件";
        this.txtPropName.Location = new System.Drawing.Point(91, 25);
        this.txtPropName.Name = "txtPropName";
        this.txtPropName.Size = new System.Drawing.Size(150, 26);
        this.txtPropName.TabIndex = 53;
        this.txtPropName.Text = "图幅代号";
        this.label1.AutoSize = true;
        this.label1.Location = new System.Drawing.Point(30, 28);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(65, 20);
        this.label1.TabIndex = 52;
        this.label1.Text = "属性名：";
        this.tpPorpRemove.BackColor = System.Drawing.Color.FromArgb(239, 255, 255);
        this.tpPorpRemove.Controls.Add(this.dgvRemoveList);
        this.tpPorpRemove.Location = new System.Drawing.Point(4, 29);
        this.tpPorpRemove.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
        this.tpPorpRemove.Name = "tpPorpRemove";
        this.tpPorpRemove.Padding = new System.Windows.Forms.Padding(3, 5, 3, 5);
        this.tpPorpRemove.Size = new System.Drawing.Size(926, 266);
        this.tpPorpRemove.TabIndex = 4;
        this.tpPorpRemove.Text = "移除属性";
        this.dgvRemoveList.AllowUserToResizeRows = false;
        this.dgvRemoveList.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
        this.dgvRemoveList.BackgroundColor = System.Drawing.Color.FromArgb(239, 255, 255);
        this.dgvRemoveList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgvRemoveList.Columns.AddRange(this.dataGridViewTextBoxColumn2);
        this.dgvRemoveList.Location = new System.Drawing.Point(10, 7);
        this.dgvRemoveList.Name = "dgvRemoveList";
        this.dgvRemoveList.RowHeadersVisible = false;
        this.dgvRemoveList.RowTemplate.Height = 23;
        this.dgvRemoveList.Size = new System.Drawing.Size(222, 251);
        this.dgvRemoveList.TabIndex = 59;
        this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        this.dataGridViewTextBoxColumn2.DataPropertyName = "Old_Name";
        this.dataGridViewTextBoxColumn2.HeaderText = "需要操作的属性列表";
        this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
        this.tpPorpTransfer.BackColor = System.Drawing.Color.FromArgb(239, 255, 255);
        this.tpPorpTransfer.Controls.Add(this.label12);
        this.tpPorpTransfer.Controls.Add(this.lblDirection);
        this.tpPorpTransfer.Controls.Add(this.label11);
        this.tpPorpTransfer.Controls.Add(this.label10);
        this.tpPorpTransfer.Controls.Add(this.dgvTransfer);
        this.tpPorpTransfer.Location = new System.Drawing.Point(4, 29);
        this.tpPorpTransfer.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
        this.tpPorpTransfer.Name = "tpPorpTransfer";
        this.tpPorpTransfer.Padding = new System.Windows.Forms.Padding(3, 5, 3, 5);
        this.tpPorpTransfer.Size = new System.Drawing.Size(926, 266);
        this.tpPorpTransfer.TabIndex = 3;
        this.tpPorpTransfer.Text = "属性转移";
        this.label12.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        this.label12.AutoSize = true;
        this.label12.ForeColor = System.Drawing.SystemColors.Highlight;
        this.label12.Location = new System.Drawing.Point(683, 194);
        this.label12.Name = "label12";
        this.label12.Size = new System.Drawing.Size(149, 20);
        this.label12.TabIndex = 57;
        this.label12.Text = "注：图纸文件自动跳过";
        this.label11.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        this.label11.BackColor = System.Drawing.Color.FromArgb(192, 255, 192);
        this.label11.Font = new System.Drawing.Font("微软雅黑", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
        this.label11.Location = new System.Drawing.Point(771, 222);
        this.label11.Name = "label11";
        this.label11.Size = new System.Drawing.Size(116, 30);
        this.label11.TabIndex = 55;
        this.label11.Text = "配置特定属性";
        this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        this.label10.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        this.label10.BackColor = System.Drawing.Color.FromArgb(255, 192, 192);
        this.label10.Font = new System.Drawing.Font("微软雅黑", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
        this.label10.Location = new System.Drawing.Point(627, 223);
        this.label10.Name = "label10";
        this.label10.Size = new System.Drawing.Size(100, 30);
        this.label10.TabIndex = 54;
        this.label10.Text = "自定义属性";
        this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        this.dgvTransfer.AllowUserToResizeRows = false;
        this.dgvTransfer.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
        this.dgvTransfer.BackgroundColor = System.Drawing.Color.FromArgb(239, 255, 255);
        this.dgvTransfer.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgvTransfer.Columns.AddRange(this.dataGridViewTextBoxColumn1);
        this.dgvTransfer.Location = new System.Drawing.Point(10, 7);
        this.dgvTransfer.Name = "dgvTransfer";
        this.dgvTransfer.RowHeadersVisible = false;
        this.dgvTransfer.RowTemplate.Height = 23;
        this.dgvTransfer.Size = new System.Drawing.Size(222, 251);
        this.dgvTransfer.TabIndex = 3;
        this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        this.dataGridViewTextBoxColumn1.DataPropertyName = "Old_Name";
        this.dataGridViewTextBoxColumn1.HeaderText = "需要操作的属性列表";
        this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
        this.pnlType.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
        this.pnlType.Controls.Add(this.label9);
        this.pnlType.Controls.Add(this.rdoAllProp);
        this.pnlType.Controls.Add(this.rdoList);
        this.pnlType.Controls.Add(this.rdoListExcept);
        this.pnlType.Location = new System.Drawing.Point(245, 330);
        this.pnlType.Name = "pnlType";
        this.pnlType.Size = new System.Drawing.Size(128, 133);
        this.pnlType.TabIndex = 58;
        this.label9.AutoSize = true;
        this.label9.ForeColor = System.Drawing.SystemColors.Highlight;
        this.label9.Location = new System.Drawing.Point(7, 12);
        this.label9.Name = "label9";
        this.label9.Size = new System.Drawing.Size(65, 20);
        this.label9.TabIndex = 48;
        this.label9.Text = "处理范围";
        this.rdoAllProp.AutoSize = true;
        this.rdoAllProp.Checked = true;
        this.rdoAllProp.Location = new System.Drawing.Point(8, 35);
        this.rdoAllProp.Name = "rdoAllProp";
        this.rdoAllProp.Size = new System.Drawing.Size(83, 24);
        this.rdoAllProp.TabIndex = 51;
        this.rdoAllProp.TabStop = true;
        this.rdoAllProp.Tag = "1";
        this.rdoAllProp.Text = "所有属性";
        this.rdoAllProp.UseVisualStyleBackColor = true;
        this.rdoList.AutoSize = true;
        this.rdoList.Location = new System.Drawing.Point(8, 65);
        this.rdoList.Name = "rdoList";
        this.rdoList.Size = new System.Drawing.Size(97, 24);
        this.rdoList.TabIndex = 52;
        this.rdoList.Tag = "2";
        this.rdoList.Text = "仅列出属性";
        this.rdoList.UseVisualStyleBackColor = true;
        this.rdoListExcept.AutoSize = true;
        this.rdoListExcept.Location = new System.Drawing.Point(8, 95);
        this.rdoListExcept.Name = "rdoListExcept";
        this.rdoListExcept.Size = new System.Drawing.Size(111, 24);
        this.rdoListExcept.TabIndex = 53;
        this.rdoListExcept.TabStop = true;
        this.rdoListExcept.Tag = "3";
        this.rdoListExcept.Text = "列出属性除外";
        this.rdoListExcept.UseVisualStyleBackColor = true;
        this.tpEditProp.BackColor = System.Drawing.Color.FromArgb(239, 255, 255);
        this.tpEditProp.Controls.Add(this.label7);
        this.tpEditProp.Controls.Add(this.cboNewValue);
        this.tpEditProp.Controls.Add(this.label6);
        this.tpEditProp.Controls.Add(this.btnAddEditRules);
        this.tpEditProp.Controls.Add(this.label5);
        this.tpEditProp.Controls.Add(this.cboEditRules);
        this.tpEditProp.Controls.Add(this.dgvEditProp);
        this.tpEditProp.Controls.Add(this.dgv_EditProp);
        this.tpEditProp.Controls.Add(this.label4);
        this.tpEditProp.Location = new System.Drawing.Point(4, 29);
        this.tpEditProp.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
        this.tpEditProp.Name = "tpEditProp";
        this.tpEditProp.Padding = new System.Windows.Forms.Padding(3, 5, 3, 5);
        this.tpEditProp.Size = new System.Drawing.Size(926, 266);
        this.tpEditProp.TabIndex = 1;
        this.tpEditProp.Text = "属性编辑";
        this.label7.AutoSize = true;
        this.label7.ForeColor = System.Drawing.Color.Brown;
        this.label7.Location = new System.Drawing.Point(730, 125);
        this.label7.Name = "label7";
        this.label7.Size = new System.Drawing.Size(189, 80);
        this.label7.TabIndex = 11;
        this.label7.Text = "6. 关键字替换：\r\n  原关键字→ 新关键字\r\n7.关键件字替换2：\r\n  原名、原关键字→ 新关键字 \r\n8. 拷贝(新名 → 旧值) \r\n 拷贝旧名称的值到新名称";
        this.cboNewValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.cboNewValue.Font = new System.Drawing.Font("微软雅黑", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
        this.cboNewValue.FormattingEnabled = true;
        this.cboNewValue.Location = new System.Drawing.Point(382, 60);
        this.cboNewValue.Margin = new System.Windows.Forms.Padding(0);
        this.cboNewValue.Name = "cboNewValue";
        this.cboNewValue.Size = new System.Drawing.Size(170, 25);
        this.cboNewValue.TabIndex = 2;
        this.label6.AutoSize = true;
        this.label6.Location = new System.Drawing.Point(730, 4);
        this.label6.Name = "label6";
        this.label6.Size = new System.Drawing.Size(182, 120);
        this.label6.TabIndex = 9;
        this.label6.Text = "修改规则：\r\n1. 改值：原名 → 新值\r\n2. 改值：原值 → 新值\r\n3. 改值：原名、原值→ 新值\r\n4. 修改属性名称\r\n5. 添加属性\r\n";
        this.btnAddEditRules.Location = new System.Drawing.Point(567, 84);
        this.btnAddEditRules.Name = "btnAddEditRules";
        this.btnAddEditRules.Size = new System.Drawing.Size(152, 26);
        this.btnAddEditRules.TabIndex = 4;
        this.btnAddEditRules.Text = "加入修改列表";
        this.btnAddEditRules.UseVisualStyleBackColor = true;
        this.btnAddEditRules.Click += new System.EventHandler(btnAddEditRules_Click);
        this.label5.AutoSize = true;
        this.label5.Location = new System.Drawing.Point(7, 84);
        this.label5.Name = "label5";
        this.label5.Size = new System.Drawing.Size(79, 20);
        this.label5.TabIndex = 7;
        this.label5.Text = "修改列表：";
        this.cboEditRules.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cboEditRules.FormattingEnabled = true;
        this.cboEditRules.Location = new System.Drawing.Point(110, 2);
        this.cboEditRules.Name = "cboEditRules";
        this.cboEditRules.Size = new System.Drawing.Size(200, 28);
        this.cboEditRules.TabIndex = 0;
        this.dgvEditProp.AllowUserToResizeColumns = false;
        this.dgvEditProp.AllowUserToResizeRows = false;
        this.dgvEditProp.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
        this.dgvEditProp.BackgroundColor = System.Drawing.Color.FromArgb(239, 255, 255);
        this.dgvEditProp.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgvEditProp.Columns.AddRange(this.OldName, this.OldValue, this.NewName, this.NewValue, this.Rule);
        this.dgvEditProp.Location = new System.Drawing.Point(4, 31);
        this.dgvEditProp.Name = "dgvEditProp";
        this.dgvEditProp.RowHeadersVisible = false;
        this.dgvEditProp.RowTemplate.Height = 25;
        this.dgvEditProp.Size = new System.Drawing.Size(715, 52);
        this.dgvEditProp.TabIndex = 1;
        this.OldName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        this.OldName.FillWeight = 80f;
        this.OldName.HeaderText = "原名称";
        this.OldName.Name = "OldName";
        this.OldName.Resizable = System.Windows.Forms.DataGridViewTriState.True;
        this.OldValue.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        this.OldValue.HeaderText = "原值";
        this.OldValue.Name = "OldValue";
        this.NewName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        this.NewName.FillWeight = 80f;
        this.NewName.HeaderText = "新名称";
        this.NewName.Name = "NewName";
        this.NewValue.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        this.NewValue.FillWeight = 120f;
        this.NewValue.HeaderText = "新值";
        this.NewValue.Name = "NewValue";
        this.NewValue.ReadOnly = true;
        this.NewValue.Resizable = System.Windows.Forms.DataGridViewTriState.True;
        this.Rule.HeaderText = "修改规则";
        this.Rule.Name = "Rule";
        this.Rule.ReadOnly = true;
        this.Rule.Width = 165;
        this.dgv_EditProp.AllowUserToResizeColumns = false;
        this.dgv_EditProp.AllowUserToResizeRows = false;
        this.dgv_EditProp.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
        this.dgv_EditProp.BackgroundColor = System.Drawing.Color.FromArgb(239, 255, 255);
        this.dgv_EditProp.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgv_EditProp.Columns.AddRange(this.Old_Name, this.Old_Value, this.New_Name, this.New_Value, this.EditType);
        this.dgv_EditProp.Location = new System.Drawing.Point(4, 111);
        this.dgv_EditProp.Name = "dgv_EditProp";
        this.dgv_EditProp.RowHeadersVisible = false;
        this.dgv_EditProp.RowTemplate.Height = 23;
        this.dgv_EditProp.Size = new System.Drawing.Size(715, 152);
        this.dgv_EditProp.TabIndex = 5;
        this.dgv_EditProp.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(dgv_EditProp_CellMouseClick);
        this.Old_Name.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        this.Old_Name.DataPropertyName = "Old_Name";
        this.Old_Name.FillWeight = 80f;
        this.Old_Name.HeaderText = "原名称";
        this.Old_Name.Name = "Old_Name";
        this.Old_Name.ReadOnly = true;
        this.Old_Value.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        this.Old_Value.DataPropertyName = "Old_Value";
        this.Old_Value.HeaderText = "原值";
        this.Old_Value.Name = "Old_Value";
        this.Old_Value.ReadOnly = true;
        this.New_Name.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        this.New_Name.DataPropertyName = "New_Name";
        this.New_Name.FillWeight = 80f;
        this.New_Name.HeaderText = "新名称";
        this.New_Name.Name = "New_Name";
        this.New_Name.ReadOnly = true;
        this.New_Value.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        this.New_Value.DataPropertyName = "New_Value";
        this.New_Value.FillWeight = 120f;
        this.New_Value.HeaderText = "新值";
        this.New_Value.Name = "New_Value";
        this.New_Value.ReadOnly = true;
        this.EditType.DataPropertyName = "EditType";
        this.EditType.HeaderText = "修改规则";
        this.EditType.Name = "EditType";
        this.EditType.ReadOnly = true;
        this.EditType.Width = 165;
        this.label4.AutoSize = true;
        this.label4.Location = new System.Drawing.Point(6, 6);
        this.label4.Name = "label4";
        this.label4.Size = new System.Drawing.Size(107, 20);
        this.label4.TabIndex = 6;
        this.label4.Text = "属性修改规则：";
        this.tabControl.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.tabControl.Controls.Add(this.tpEditProp);
        this.tabControl.Controls.Add(this.tpPorpTransfer);
        this.tabControl.Controls.Add(this.tpPorpRemove);
        this.tabControl.Controls.Add(this.tpAddFrame);
        this.tabControl.Controls.Add(this.tbAddProperty);
        this.tabControl.Location = new System.Drawing.Point(3, 288);
        this.tabControl.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
        this.tabControl.Name = "tabControl";
        this.tabControl.Padding = new System.Drawing.Point(20, 3);
        this.tabControl.SelectedIndex = 0;
        this.tabControl.Size = new System.Drawing.Size(934, 299);
        this.tabControl.TabIndex = 2;
        this.tabControl.SelectedIndexChanged += new System.EventHandler(tabControl_SelectedIndexChanged);
        this.tbAddProperty.BackColor = System.Drawing.Color.FromArgb(239, 255, 255);
        this.tbAddProperty.Controls.Add(this.dgvAddPropertyList);
        this.tbAddProperty.Controls.Add(this.label13);
        this.tbAddProperty.Controls.Add(this.btnAddlist);
        this.tbAddProperty.Controls.Add(this.cmbPropertyValue);
        this.tbAddProperty.Controls.Add(this.linkLabel1);
        this.tbAddProperty.Controls.Add(this.rdbcover);
        this.tbAddProperty.Controls.Add(this.rdbskip);
        this.tbAddProperty.Controls.Add(this.label8);
        this.tbAddProperty.Controls.Add(this.dgvAddProperty);
        this.tbAddProperty.Location = new System.Drawing.Point(4, 29);
        this.tbAddProperty.Name = "tbAddProperty";
        this.tbAddProperty.Padding = new System.Windows.Forms.Padding(3);
        this.tbAddProperty.Size = new System.Drawing.Size(926, 266);
        this.tbAddProperty.TabIndex = 7;
        this.tbAddProperty.Text = "添加属性";
        this.dgvAddPropertyList.AllowUserToResizeColumns = false;
        this.dgvAddPropertyList.AllowUserToResizeRows = false;
        this.dgvAddPropertyList.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
        this.dgvAddPropertyList.BackgroundColor = System.Drawing.Color.FromArgb(239, 255, 255);
        this.dgvAddPropertyList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgvAddPropertyList.Columns.AddRange(this.dataGridViewTextBoxColumn3, this.dataGridViewTextBoxColumn7);
        this.dgvAddPropertyList.Location = new System.Drawing.Point(5, 96);
        this.dgvAddPropertyList.Name = "dgvAddPropertyList";
        this.dgvAddPropertyList.RowHeadersVisible = false;
        this.dgvAddPropertyList.RowTemplate.Height = 23;
        this.dgvAddPropertyList.Size = new System.Drawing.Size(419, 164);
        this.dgvAddPropertyList.TabIndex = 8;
        this.dgvAddPropertyList.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(dgvAddPropertyList_CellMouseClick);
        this.dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        this.dataGridViewTextBoxColumn3.DataPropertyName = "PropertyName";
        this.dataGridViewTextBoxColumn3.FillWeight = 239.0625f;
        this.dataGridViewTextBoxColumn3.HeaderText = "属性名称";
        this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
        this.dataGridViewTextBoxColumn3.ReadOnly = true;
        this.dataGridViewTextBoxColumn7.DataPropertyName = "Rule";
        this.dataGridViewTextBoxColumn7.HeaderText = "属性值规则";
        this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
        this.dataGridViewTextBoxColumn7.ReadOnly = true;
        this.dataGridViewTextBoxColumn7.Width = 215;
        this.label13.AutoSize = true;
        this.label13.Location = new System.Drawing.Point(6, 70);
        this.label13.Name = "label13";
        this.label13.Size = new System.Drawing.Size(107, 20);
        this.label13.TabIndex = 7;
        this.label13.Text = "属性添加列表：";
        this.btnAddlist.Location = new System.Drawing.Point(272, 62);
        this.btnAddlist.Name = "btnAddlist";
        this.btnAddlist.Size = new System.Drawing.Size(152, 26);
        this.btnAddlist.TabIndex = 5;
        this.btnAddlist.Text = "加入添加列表";
        this.btnAddlist.UseVisualStyleBackColor = true;
        this.btnAddlist.Click += new System.EventHandler(btnAddlist_Click);
        this.cmbPropertyValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.cmbPropertyValue.Font = new System.Drawing.Font("微软雅黑", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
        this.cmbPropertyValue.FormattingEnabled = true;
        this.cmbPropertyValue.Location = new System.Drawing.Point(209, 35);
        this.cmbPropertyValue.Margin = new System.Windows.Forms.Padding(0);
        this.cmbPropertyValue.Name = "cmbPropertyValue";
        this.cmbPropertyValue.Size = new System.Drawing.Size(213, 25);
        this.cmbPropertyValue.TabIndex = 4;
        this.linkLabel1.AutoSize = true;
        this.linkLabel1.Location = new System.Drawing.Point(432, 93);
        this.linkLabel1.Name = "linkLabel1";
        this.linkLabel1.Size = new System.Drawing.Size(65, 20);
        this.linkLabel1.TabIndex = 3;
        this.linkLabel1.TabStop = true;
        this.linkLabel1.Text = "查看说明";
        this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(linkLabel1_LinkClicked);
        this.rdbcover.AutoSize = true;
        this.rdbcover.Checked = true;
        this.rdbcover.Location = new System.Drawing.Point(548, 39);
        this.rdbcover.Name = "rdbcover";
        this.rdbcover.Size = new System.Drawing.Size(55, 24);
        this.rdbcover.TabIndex = 2;
        this.rdbcover.TabStop = true;
        this.rdbcover.Text = "覆盖";
        this.rdbcover.UseVisualStyleBackColor = true;
        this.rdbskip.AutoSize = true;
        this.rdbskip.Location = new System.Drawing.Point(548, 7);
        this.rdbskip.Name = "rdbskip";
        this.rdbskip.Size = new System.Drawing.Size(55, 24);
        this.rdbskip.TabIndex = 2;
        this.rdbskip.Text = "跳过";
        this.rdbskip.UseVisualStyleBackColor = true;
        this.label8.AutoSize = true;
        this.label8.Location = new System.Drawing.Point(432, 7);
        this.label8.Name = "label8";
        this.label8.Size = new System.Drawing.Size(110, 20);
        this.label8.TabIndex = 1;
        this.label8.Text = "属性已经存在时:";
        this.dgvAddProperty.AllowUserToResizeColumns = false;
        this.dgvAddProperty.AllowUserToResizeRows = false;
        this.dgvAddProperty.BackgroundColor = System.Drawing.Color.White;
        this.dgvAddProperty.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgvAddProperty.Columns.AddRange(this.propertyName, this.propertyValue);
        this.dgvAddProperty.Location = new System.Drawing.Point(6, 7);
        this.dgvAddProperty.Name = "dgvAddProperty";
        this.dgvAddProperty.RowHeadersVisible = false;
        this.dgvAddProperty.RowTemplate.Height = 23;
        this.dgvAddProperty.Size = new System.Drawing.Size(418, 53);
        this.dgvAddProperty.TabIndex = 1;
        this.propertyName.HeaderText = "属性名称";
        this.propertyName.Name = "propertyName";
        this.propertyName.Width = 200;
        this.propertyValue.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        this.propertyValue.HeaderText = "属性值规则";
        this.propertyValue.Name = "propertyValue";
        this.propertyValue.Resizable = System.Windows.Forms.DataGridViewTriState.True;
        this.btnAddDrwNum.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
        this.btnAddDrwNum.BackColor = System.Drawing.SystemColors.ButtonFace;
        this.btnAddDrwNum.FlatAppearance.BorderColor = System.Drawing.SystemColors.ActiveBorder;
        this.btnAddDrwNum.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnAddDrwNum.Location = new System.Drawing.Point(484, 289);
        this.btnAddDrwNum.Name = "btnAddDrwNum";
        this.btnAddDrwNum.Size = new System.Drawing.Size(95, 27);
        this.btnAddDrwNum.TabIndex = 4;
        this.btnAddDrwNum.Text = "添加图号";
        this.btnAddDrwNum.TextAlign = System.Drawing.ContentAlignment.TopCenter;
        this.btnAddDrwNum.UseVisualStyleBackColor = false;
        this.btnAddDrwNum.Click += new System.EventHandler(btnAddDrwNum_Click);
        base.AutoScaleDimensions = new System.Drawing.SizeF(8f, 20f);
        base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.BackColor = System.Drawing.Color.FromArgb(239, 255, 255);
        base.ClientSize = new System.Drawing.Size(1088, 591);
        base.Controls.Add(this.btnAddDrwNum);
        base.Controls.Add(this.pnlType);
        base.Controls.Add(this.groupBox);
        base.Controls.Add(this.tabControl);
        base.Controls.Add(this.dataGridView);
        base.Controls.Add(this.menuStrip);
        this.Font = new System.Drawing.Font("微软雅黑", 10.5f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
        base.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
        base.Name = "FrmMain";
        this.Text = "属性批量编辑工具 By:郭小鸟";
        base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(FrmMain_FormClosing);
        base.Load += new System.EventHandler(FrmMain_Load);
        this.menuStrip.ResumeLayout(false);
        this.menuStrip.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)this.dataGridView).EndInit();
        this.groupBox.ResumeLayout(false);
        this.groupBox.PerformLayout();
        this.tpAddFrame.ResumeLayout(false);
        this.tpAddFrame.PerformLayout();
        this.tpPorpRemove.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)this.dgvRemoveList).EndInit();
        this.tpPorpTransfer.ResumeLayout(false);
        this.tpPorpTransfer.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)this.dgvTransfer).EndInit();
        this.pnlType.ResumeLayout(false);
        this.pnlType.PerformLayout();
        this.tpEditProp.ResumeLayout(false);
        this.tpEditProp.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)this.dgvEditProp).EndInit();
        ((System.ComponentModel.ISupportInitialize)this.dgv_EditProp).EndInit();
        this.tabControl.ResumeLayout(false);
        this.tbAddProperty.ResumeLayout(false);
        this.tbAddProperty.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)this.dgvAddPropertyList).EndInit();
        ((System.ComponentModel.ISupportInitialize)this.dgvAddProperty).EndInit();
        base.ResumeLayout(false);
        base.PerformLayout();
    }
}
