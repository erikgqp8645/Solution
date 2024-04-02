using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PropertyEditingTool;

public class ControlHelper
{
	public static ContextMenuStrip NewContextMenuStrip(List<string> text, List<MouseEventHandler> mouseUp)
	{
		ContextMenuStrip objContextMenuStrip = new ContextMenuStrip();
		objContextMenuStrip.Size = new Size(153, 48);
		for (int i = 0; i < text.Count; i++)
		{
			ToolStripMenuItem ToolStripMenuItem = new ToolStripMenuItem();
			objContextMenuStrip.Items.AddRange(new ToolStripItem[1] { ToolStripMenuItem });
			ToolStripMenuItem.Name = string.Concat(text, "ToolStripMenuItem");
			ToolStripMenuItem.Size = new Size(152, 22);
			ToolStripMenuItem.Text = text[i];
			ToolStripMenuItem.MouseUp += mouseUp[i].Invoke;
		}
		return objContextMenuStrip;
	}

	public static ContextMenuStrip NewContextMenuStrip(string text, MouseEventHandler mouseUp)
	{
		ContextMenuStrip objContextMenuStrip = new ContextMenuStrip();
		objContextMenuStrip.Size = new Size(153, 48);
		ToolStripMenuItem ToolStripMenuItem = new ToolStripMenuItem();
		objContextMenuStrip.Items.AddRange(new ToolStripItem[1] { ToolStripMenuItem });
		ToolStripMenuItem.Name = text + "ToolStripMenuItem";
		ToolStripMenuItem.Size = new Size(152, 22);
		ToolStripMenuItem.Text = text;
		ToolStripMenuItem.MouseUp += mouseUp.Invoke;
		return objContextMenuStrip;
	}

	public static string[] FilesSelectionBoxs(string fileType)
	{
		OpenFileDialog filelog = new OpenFileDialog();
		filelog.Multiselect = true;
		filelog.Title = "请选择文件";
		filelog.Filter = fileType;
		if (filelog.ShowDialog() == DialogResult.OK)
		{
			return filelog.FileNames;
		}
		return null;
	}

	public static string FileSelectionBoxs(string fileType)
	{
		OpenFileDialog filelog = new OpenFileDialog();
		filelog.Multiselect = false;
		filelog.Title = "请选择文件";
		filelog.Filter = fileType;
		if (filelog.ShowDialog() == DialogResult.OK)
		{
			return filelog.FileName;
		}
		return null;
	}

	public static string OpenfolderDialog()
	{
		FolderBrowserDialog dilog = new FolderBrowserDialog();
		dilog.Description = "请选择文件夹";
		if (dilog.ShowDialog() == DialogResult.Cancel)
		{
			return null;
		}
		return dilog.SelectedPath;
	}

	public static void GetSubFile(string floderPath, int count, ref List<string> lsFiles, string fileType)
	{
		if (count >= 3)
		{
			return;
		}
		try
		{
			DirectoryInfo dir = new DirectoryInfo(floderPath);
			FileInfo[] files = dir.GetFiles(fileType, SearchOption.TopDirectoryOnly);
			foreach (FileInfo item in files)
			{
				lsFiles.Add(item.FullName);
			}
			DirectoryInfo[] dirs = dir.GetDirectories();
			if (dirs != null && dirs.Length != 0)
			{
				count++;
				DirectoryInfo[] array = dirs;
				for (int i = 0; i < array.Length; i++)
				{
					GetSubFile(array[i].FullName, count, ref lsFiles, fileType);
				}
			}
		}
		catch (Exception)
		{
			lsFiles = new List<string>();
		}
	}
}
