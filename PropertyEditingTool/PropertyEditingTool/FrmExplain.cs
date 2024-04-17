using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PropertyEditingTool;

public class FrmExplain : Form
{
	private IContainer components;

	private TextBox txtExplain;

	public FrmExplain()
	{
		InitializeComponent();
		txtExplain.ReadOnly = true;
		txtExplain.Text = "$文件名中的代号\r\n$文件名中的名称\r\n$文件名\r\n$文件名[-][1]     说明：将文件名以'-'分割后的第一段字符,如:MD001-零件1.sldprt 分割后字符串为MD001\r\n$密度\r\n$材料\r\n$体积\r\n$表面积\r\n$配置名\r\n$短日期\r\n$PRP:'SW - 文件名称(File Name)'\r\n$PRP:'SW - Configuration Name'\r\n$PRP:'SW - Author'\r\n$PRP:'SW - Title'\r\n$PRP:'SW - Created Date'\r\n$PRP:'SW - Last Saved Date'\r\n组合属性";
		txtExplain.Select(0, 0);
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
		this.txtExplain = new System.Windows.Forms.TextBox();
		base.SuspendLayout();
		this.txtExplain.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.txtExplain.Font = new System.Drawing.Font("宋体", 12f);
		this.txtExplain.Location = new System.Drawing.Point(8, 8);
		this.txtExplain.Multiline = true;
		this.txtExplain.Name = "txtExplain";
		this.txtExplain.Size = new System.Drawing.Size(478, 301);
		this.txtExplain.TabIndex = 1;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(494, 315);
		base.Controls.Add(this.txtExplain);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "FrmExplain";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "说明";
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
