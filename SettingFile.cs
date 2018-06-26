using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace RunMSTest
{
	public class SettingFile : Form
	{
		private string rTestName = "Test Name";

		private string rTestGUID = "Test GUID";

		private string rTestPriority = "Test Priority";

		private string rTestOwner = "Test Owner";

		private string rTestCategory = "Test Category";

		private string rExecute = "Execute";

		private string txtfilepath = "";

		private IContainer components;

		private Label label1;

		private Label label2;

		private Label label3;

		private TextBox DLLFileText;

		private TextBox TestSettings;

		private TextBox MSTestFile;

		private Button DLLSelect;

		private Button SetingSelect;

		private Button MSTSelect;

		private DataGridView TestMethodGrid;

		private Button GetTest;

		private Label label4;

		private ComboBox TestProperty;

		private TextBox PropertyValue;

		private RadioButton SelectAll;

		private RadioButton UnselectAll;

		private Button Execute;

		private Label label5;

		private Button SelectTxt;

		private CheckBox chkRestart;

		public SettingFile()
		{
			this.InitializeComponent();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && this.components != null)
			{
				this.components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void DLLSelect_Click(object sender, EventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog()
			{
				Filter = "Dll File (*.dll)|*.dll",
				FilterIndex = 1
			};
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				this.DLLFileText.Text = dialog.FileName;
			}
		}

		private void Execute_Click(object sender, EventArgs e)
		{
			ProcessStartInfo start;
			int count = 0;
			if (this.DLLFileText.Text == "")
			{
				MessageBox.Show("Please select a dll file!");
				return;
			}
			if (this.MSTestFile.Text == "")
			{
				MessageBox.Show("Please select a MSTest file!");
				return;
			}
			XmlDocument ordertest = new XmlDocument();
			XmlDeclaration dec = ordertest.CreateXmlDeclaration("1.0", "UTF-8", "");
			ordertest.AppendChild(dec);
			XmlNode root = ordertest.CreateElement("OrderedTest");
			this.OrderedTestHeader(ordertest, ref root);
			XmlNode links = ordertest.CreateElement("TestLinks");
			root.AppendChild(links);
			foreach (DataGridViewRow row in (IEnumerable)this.TestMethodGrid.Rows)
			{
				if (!Convert.ToBoolean((row.Cells[this.rExecute] as DataGridViewCheckBoxCell).Value))
				{
					continue;
				}
				XmlNode link = ordertest.CreateElement("TestLink");
				this.TestLinkNode(ordertest, row, ref link);
				links.AppendChild(link);
				count++;
			}
			ordertest.AppendChild(root);
			string orderfile = string.Concat(Path.GetTempPath(), "test.orderedtest");
			ordertest.Save(orderfile);
			if (count <= 0)
			{
				MessageBox.Show("Please select at one test case for running!");
			}
			else
			{
				Process myprocess = new Process();
				string resultfolder = Path.GetDirectoryName(this.DLLFileText.Text);
				string resultfile = Path.GetFileNameWithoutExtension(this.DLLFileText.Text);
				if (this.TestSettings.Text == "")
				{
					string text = this.MSTestFile.Text;
					string[] str = new string[] { "/testcontainer:\"", orderfile, "\" /resultsfile:\"", resultfolder, "\\", resultfile, null, null };
					str[6] = DateTime.Now.ToString("yyyyMMMddhhmmss");
					str[7] = ".trx\"";
					start = new ProcessStartInfo(text, string.Concat(str));
				}
				else
				{
					string text1 = this.MSTestFile.Text;
					string[] strArrays = new string[] { "/testcontainer:\"", orderfile, "\" /testsettings:\"", this.TestSettings.Text, "\" /resultsfile:\"", resultfolder, "\\", resultfile, null, null };
					strArrays[8] = DateTime.Now.ToString("yyyyMMMddhhmmss");
					strArrays[9] = ".trx\"";
					start = new ProcessStartInfo(text1, string.Concat(strArrays));
				}
				myprocess.StartInfo = start;
				myprocess.Start();
				myprocess.WaitForExit(2147483647);
				if (!this.chkRestart.Checked)
				{
					string[] str1 = new string[] { "The result file is ", resultfolder, "\\", resultfile, null, null };
					str1[4] = DateTime.Now.ToString("yyyyMMMddhhmmss");
					str1[5] = ".trx";
					MessageBox.Show(string.Concat(str1), "Done!");
				}
				else
				{
					start = new ProcessStartInfo()
					{
						WindowStyle = ProcessWindowStyle.Hidden,
						FileName = "cmd",
						Arguments = "/C shutdown -r -f -t 300"
					};
					Process.Start(start);
				}
				myprocess.Dispose();
			}
			File.Delete(orderfile);
		}

		private DataTable GetAllTestMethod(Assembly ass)
		{
			Type[] types = ass.GetTypes();
			DataTable dt = this.TestMethodData();
			Type[] typeArray = types;
			for (int i = 0; i < (int)typeArray.Length; i++)
			{
				MemberInfo[] methods = typeArray[i].GetMethods();
				for (int j = 0; j < (int)methods.Length; j++)
				{
					MemberInfo m = methods[j];
					DataRow row = dt.NewRow();
					this.GetTestMethodData(m, ref row);
					if (row[this.rTestName].ToString() != "")
					{
						dt.Rows.Add(row);
					}
				}
			}
			return dt;
		}

		private void GetTest_Click(object sender, EventArgs e)
		{
			Assembly ass;
			DataTable dt;
			this.TestMethodGrid.DataSource = null;
			this.TestMethodGrid.Rows.Clear();
			this.TestMethodGrid.Columns.Clear();
			if (this.DLLFileText.Text == "")
			{
				MessageBox.Show("Please select a dll file!");
				return;
			}
			try
			{
				ass = Assembly.LoadFrom(this.DLLFileText.Text);
			}
			catch
			{
				MessageBox.Show("Can't load the dll file!");
				return;
			}
			this.SelectAll.Enabled = true;
			this.UnselectAll.Enabled = true;
			this.SelectAll.Checked = true;
            string newtext = ""; 
			string str = this.TestProperty.SelectedItem.ToString();
			string str1 = str;
			if (str == null)
			{
				dt = this.GetAllTestMethod(ass);
				if (dt != null)
				{
					this.ShowTestData(dt);
				}
				return;
			}
			else if (str1 == "Priority")
			{
				dt = this.GetTestMethodPriority(ass);
			}
			else if (str1 == "Owner")
			{
				dt = this.GetTestMethodOwner(ass);
			}
			else if (str1 == "Test Category")
			{
				dt = this.GetTestMethodCategoty(ass);
			}
			else
			{
				if (str1 != "Text")
				{
					dt = this.GetAllTestMethod(ass);
					if (dt != null)
					{
						this.ShowTestData(dt);
					}
					return;
				}
				dt = this.GetTestMethodText(ass);
			}
			if (dt != null)
			{
				this.ShowTestData(dt);
			}
		}

		private DataTable GetTestMethodCategoty(Assembly ass)
		{
			if (this.PropertyValue.Text == "")
			{
				MessageBox.Show("Please provide the property value!");
				return null;
			}
			Type[] types = ass.GetTypes();
			DataTable dt = this.TestMethodData();
			Type[] typeArray = types;
			for (int i = 0; i < (int)typeArray.Length; i++)
			{
				MemberInfo[] methods = typeArray[i].GetMethods();
				for (int j = 0; j < (int)methods.Length; j++)
				{
					MemberInfo m = methods[j];
					DataRow row = dt.NewRow();
					this.GetTestMethodData(m, ref row);
					if (row[this.rTestName].ToString() != "" && row[this.rTestCategory].ToString().Contains(this.PropertyValue.Text))
					{
						dt.Rows.Add(row);
					}
				}
			}
			return dt;
		}

		private void GetTestMethodData(MemberInfo member, ref DataRow row)
		{
			this.TestMethodData();
			object[] attributes = member.GetCustomAttributes(true);
			bool testmethod = false;
			object[] objArray = attributes;
			int num = 0;
			while (num < (int)objArray.Length)
			{
				if (!(objArray[num] is TestMethodAttribute))
				{
					num++;
				}
				else
				{
					testmethod = true;
					break;
				}
			}
			if (testmethod)
			{
				row[this.rTestName] = member.Name;
				HashAlgorithm Provider = new SHA1CryptoServiceProvider();
				byte[] hash = Provider.ComputeHash(Encoding.Unicode.GetBytes(string.Concat(member.DeclaringType.FullName, ".", member.Name)));
				byte[] to = new byte[16];
				Array.Copy(hash, to, 16);
				string str = this.rTestGUID;
				Guid guid = new Guid(to);
				row[str] = guid.ToString();
				string p = "";
				string o = "";
				string c = "";
				object[] objArray1 = attributes;
				for (int i = 0; i < (int)objArray1.Length; i++)
				{
					object att = objArray1[i];
					PriorityAttribute pri = att as PriorityAttribute;
					OwnerAttribute own = att as OwnerAttribute;
					TestCategoryAttribute cate = att as TestCategoryAttribute;
					if (pri != null)
					{
						p = pri.Priority.ToString();
					}
					if (own != null)
					{
						o = own.Owner;
					}
					if (cate != null)
					{
						foreach (string ca in cate.TestCategories)
						{
							c = string.Concat(c, ca, ";");
						}
					}
				}
				row[this.rTestPriority] = p;
				row[this.rTestOwner] = o;
				if (c.Length > 0)
				{
					row[this.rTestCategory] = c.Remove(c.Length - 1);
				}
			}
		}

		private DataTable GetTestMethodOwner(Assembly ass)
		{
			if (this.PropertyValue.Text == "")
			{
				MessageBox.Show("Please provide the property value!");
				return null;
			}
			Type[] types = ass.GetTypes();
			DataTable dt = this.TestMethodData();
			Type[] typeArray = types;
			for (int i = 0; i < (int)typeArray.Length; i++)
			{
				MemberInfo[] methods = typeArray[i].GetMethods();
				for (int j = 0; j < (int)methods.Length; j++)
				{
					MemberInfo m = methods[j];
					DataRow row = dt.NewRow();
					this.GetTestMethodData(m, ref row);
					if (row[this.rTestName].ToString() != "" && row[this.rTestOwner].ToString() == this.PropertyValue.Text)
					{
						dt.Rows.Add(row);
					}
				}
			}
			return dt;
		}

		private DataTable GetTestMethodPriority(Assembly ass)
		{
			if (this.PropertyValue.Text == "")
			{
				MessageBox.Show("Please provide the property value!");
				return null;
			}
			Type[] types = ass.GetTypes();
			DataTable dt = this.TestMethodData();
			Type[] typeArray = types;
			for (int i = 0; i < (int)typeArray.Length; i++)
			{
				MemberInfo[] methods = typeArray[i].GetMethods();
				for (int j = 0; j < (int)methods.Length; j++)
				{
					MemberInfo m = methods[j];
					DataRow row = dt.NewRow();
					this.GetTestMethodData(m, ref row);
					if (row[this.rTestName].ToString() != "" && row[this.rTestPriority].ToString() == this.PropertyValue.Text)
					{
						dt.Rows.Add(row);
					}
				}
			}
			return dt;
		}

		private DataTable GetTestMethodText(Assembly ass)
		{
			if (this.txtfilepath == "")
			{
				MessageBox.Show("Please provide the property value!");
				return null;
			}
			string[] lines = File.ReadAllLines(this.txtfilepath);
			Type[] types = ass.GetTypes();
			DataTable dt = this.TestMethodData();
			Type[] typeArray = types;
			for (int i = 0; i < (int)typeArray.Length; i++)
			{
				MemberInfo[] methods = typeArray[i].GetMethods();
				for (int j = 0; j < (int)methods.Length; j++)
				{
					MemberInfo m = methods[j];
					DataRow row = dt.NewRow();
					this.GetTestMethodData(m, ref row);
					string[] strArrays = lines;
					for (int k = 0; k < (int)strArrays.Length; k++)
					{
						string str = strArrays[k];
						if (row[this.rTestName].ToString().Contains(str))
						{
							dt.Rows.Add(row);
						}
					}
				}
			}
			return dt;
		}

		private void InitializeComponent()
		{
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.DLLFileText = new System.Windows.Forms.TextBox();
            this.TestSettings = new System.Windows.Forms.TextBox();
            this.MSTestFile = new System.Windows.Forms.TextBox();
            this.DLLSelect = new System.Windows.Forms.Button();
            this.SetingSelect = new System.Windows.Forms.Button();
            this.MSTSelect = new System.Windows.Forms.Button();
            this.TestMethodGrid = new System.Windows.Forms.DataGridView();
            this.GetTest = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.TestProperty = new System.Windows.Forms.ComboBox();
            this.PropertyValue = new System.Windows.Forms.TextBox();
            this.SelectAll = new System.Windows.Forms.RadioButton();
            this.UnselectAll = new System.Windows.Forms.RadioButton();
            this.Execute = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.SelectTxt = new System.Windows.Forms.Button();
            this.chkRestart = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.TestMethodGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "DLL File";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(83, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Test Setting File";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 67);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(63, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "MSTest File";
            // 
            // DLLFileText
            // 
            this.DLLFileText.Enabled = false;
            this.DLLFileText.Location = new System.Drawing.Point(97, 6);
            this.DLLFileText.Name = "DLLFileText";
            this.DLLFileText.Size = new System.Drawing.Size(238, 20);
            this.DLLFileText.TabIndex = 3;
            this.DLLFileText.TextChanged += new System.EventHandler(this.DLLFileText_TextChanged);
            // 
            // TestSettings
            // 
            this.TestSettings.Enabled = false;
            this.TestSettings.Location = new System.Drawing.Point(97, 36);
            this.TestSettings.Name = "TestSettings";
            this.TestSettings.Size = new System.Drawing.Size(238, 20);
            this.TestSettings.TabIndex = 4;
            // 
            // MSTestFile
            // 
            this.MSTestFile.Enabled = false;
            this.MSTestFile.Location = new System.Drawing.Point(97, 65);
            this.MSTestFile.Name = "MSTestFile";
            this.MSTestFile.Size = new System.Drawing.Size(238, 20);
            this.MSTestFile.TabIndex = 5;
            // 
            // DLLSelect
            // 
            this.DLLSelect.Location = new System.Drawing.Point(341, 4);
            this.DLLSelect.Name = "DLLSelect";
            this.DLLSelect.Size = new System.Drawing.Size(112, 23);
            this.DLLSelect.TabIndex = 6;
            this.DLLSelect.Text = "Select &DLL File";
            this.DLLSelect.UseVisualStyleBackColor = true;
            this.DLLSelect.Click += new System.EventHandler(this.DLLSelect_Click);
            // 
            // SetingSelect
            // 
            this.SetingSelect.Location = new System.Drawing.Point(342, 33);
            this.SetingSelect.Name = "SetingSelect";
            this.SetingSelect.Size = new System.Drawing.Size(111, 23);
            this.SetingSelect.TabIndex = 7;
            this.SetingSelect.Text = "Select &Setting File";
            this.SetingSelect.UseVisualStyleBackColor = true;
            this.SetingSelect.Click += new System.EventHandler(this.SetingSelect_Click);
            // 
            // MSTSelect
            // 
            this.MSTSelect.Location = new System.Drawing.Point(342, 62);
            this.MSTSelect.Name = "MSTSelect";
            this.MSTSelect.Size = new System.Drawing.Size(111, 23);
            this.MSTSelect.TabIndex = 8;
            this.MSTSelect.Text = "Select &MSTest File";
            this.MSTSelect.UseVisualStyleBackColor = true;
            this.MSTSelect.Click += new System.EventHandler(this.MSTSelect_Click);
            // 
            // TestMethodGrid
            // 
            this.TestMethodGrid.AllowUserToAddRows = false;
            this.TestMethodGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.TestMethodGrid.Location = new System.Drawing.Point(15, 148);
            this.TestMethodGrid.Name = "TestMethodGrid";
            this.TestMethodGrid.Size = new System.Drawing.Size(438, 273);
            this.TestMethodGrid.TabIndex = 9;
            this.TestMethodGrid.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.TestMethodGrid_CellContentClick);
            this.TestMethodGrid.Sorted += new System.EventHandler(this.TestMethodGrid_Sorted);
            // 
            // GetTest
            // 
            this.GetTest.Location = new System.Drawing.Point(342, 92);
            this.GetTest.Name = "GetTest";
            this.GetTest.Size = new System.Drawing.Size(111, 23);
            this.GetTest.TabIndex = 10;
            this.GetTest.Text = "&Get Test Method";
            this.GetTest.UseVisualStyleBackColor = true;
            this.GetTest.Click += new System.EventHandler(this.GetTest_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 97);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(70, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "Test Property";
            // 
            // TestProperty
            // 
            this.TestProperty.FormattingEnabled = true;
            this.TestProperty.Items.AddRange(new object[] {
            "All",
            "Nada"});
            this.TestProperty.Location = new System.Drawing.Point(98, 94);
            this.TestProperty.Name = "TestProperty";
            this.TestProperty.Size = new System.Drawing.Size(115, 21);
            this.TestProperty.TabIndex = 12;
            this.TestProperty.Text = "All";
            this.TestProperty.SelectedIndexChanged += new System.EventHandler(this.TestProperty_SelectedIndexChanged);
            // 
            // PropertyValue
            // 
            this.PropertyValue.Enabled = false;
            this.PropertyValue.Location = new System.Drawing.Point(219, 94);
            this.PropertyValue.Name = "PropertyValue";
            this.PropertyValue.Size = new System.Drawing.Size(116, 20);
            this.PropertyValue.TabIndex = 13;
            // 
            // SelectAll
            // 
            this.SelectAll.AutoSize = true;
            this.SelectAll.Checked = true;
            this.SelectAll.Enabled = false;
            this.SelectAll.Location = new System.Drawing.Point(16, 123);
            this.SelectAll.Name = "SelectAll";
            this.SelectAll.Size = new System.Drawing.Size(69, 17);
            this.SelectAll.TabIndex = 14;
            this.SelectAll.TabStop = true;
            this.SelectAll.Text = "Select &All";
            this.SelectAll.UseVisualStyleBackColor = true;
            this.SelectAll.CheckedChanged += new System.EventHandler(this.SelectAll_CheckedChanged);
            // 
            // UnselectAll
            // 
            this.UnselectAll.AutoSize = true;
            this.UnselectAll.Enabled = false;
            this.UnselectAll.Location = new System.Drawing.Point(91, 123);
            this.UnselectAll.Name = "UnselectAll";
            this.UnselectAll.Size = new System.Drawing.Size(81, 17);
            this.UnselectAll.TabIndex = 15;
            this.UnselectAll.Text = "&Unselect All";
            this.UnselectAll.UseVisualStyleBackColor = true;
            this.UnselectAll.CheckedChanged += new System.EventHandler(this.UnselectAll_CheckedChanged);
            // 
            // Execute
            // 
            this.Execute.Location = new System.Drawing.Point(342, 120);
            this.Execute.Name = "Execute";
            this.Execute.Size = new System.Drawing.Size(111, 23);
            this.Execute.TabIndex = 16;
            this.Execute.Text = "E&xecute Test";
            this.Execute.UseVisualStyleBackColor = true;
            this.Execute.Click += new System.EventHandler(this.Execute_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(308, 435);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(145, 13);
            this.label5.TabIndex = 18;
            this.label5.Text = "Copyright © 2015 Lan Huynh";
            // 
            // SelectTxt
            // 
            this.SelectTxt.Location = new System.Drawing.Point(219, 92);
            this.SelectTxt.Name = "SelectTxt";
            this.SelectTxt.Size = new System.Drawing.Size(116, 23);
            this.SelectTxt.TabIndex = 19;
            this.SelectTxt.Text = "Select &Text File";
            this.SelectTxt.UseVisualStyleBackColor = true;
            this.SelectTxt.Visible = false;
            this.SelectTxt.Click += new System.EventHandler(this.SelectTxt_Click);
            // 
            // chkRestart
            // 
            this.chkRestart.AutoSize = true;
            this.chkRestart.Location = new System.Drawing.Point(15, 434);
            this.chkRestart.Name = "chkRestart";
            this.chkRestart.Size = new System.Drawing.Size(107, 17);
            this.chkRestart.TabIndex = 20;
            this.chkRestart.Text = "&Restart after Run";
            this.chkRestart.UseVisualStyleBackColor = true;
            // 
            // SettingFile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(465, 457);
            this.Controls.Add(this.chkRestart);
            this.Controls.Add(this.SelectTxt);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.Execute);
            this.Controls.Add(this.UnselectAll);
            this.Controls.Add(this.SelectAll);
            this.Controls.Add(this.PropertyValue);
            this.Controls.Add(this.TestProperty);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.GetTest);
            this.Controls.Add(this.TestMethodGrid);
            this.Controls.Add(this.MSTSelect);
            this.Controls.Add(this.SetingSelect);
            this.Controls.Add(this.DLLSelect);
            this.Controls.Add(this.MSTestFile);
            this.Controls.Add(this.TestSettings);
            this.Controls.Add(this.DLLFileText);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "SettingFile";
            this.Text = "Run MS Test";
            ((System.ComponentModel.ISupportInitialize)(this.TestMethodGrid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		private void MSTSelect_Click(object sender, EventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog()
			{
				Filter = "Execute file (*.exe)|*.exe",
				FilterIndex = 1
			};
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				this.MSTestFile.Text = dialog.FileName;
			}
		}

		private void OrderedTestHeader(XmlDocument parent, ref XmlNode root)
		{
			XmlAttribute att = parent.CreateAttribute("name");
			att.Value = "OrderedTest";
			XmlAttribute storage = parent.CreateAttribute("storage");
			storage.Value = this.DLLFileText.Text;
			XmlAttribute id = parent.CreateAttribute("id");
			id.Value = Guid.NewGuid().ToString();
			XmlAttribute continueAfterFailure = parent.CreateAttribute("continueAfterFailure");
			continueAfterFailure.Value = "true";
			XmlAttribute xmlns = parent.CreateAttribute("xmlns");
			xmlns.Value = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
			root.Attributes.Append(att);
			root.Attributes.Append(storage);
			root.Attributes.Append(id);
			root.Attributes.Append(continueAfterFailure);
			root.Attributes.Append(xmlns);
		}

		private void SelectAll_CheckedChanged(object sender, EventArgs e)
		{
			foreach (DataGridViewRow row in (IEnumerable)this.TestMethodGrid.Rows)
			{
				(row.Cells[this.rExecute] as DataGridViewCheckBoxCell).Value = true;
			}
		}

		private void SelectTxt_Click(object sender, EventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog()
			{
				Filter = "Text File (*.txt)|*.txt",
				FilterIndex = 1
			};
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				this.txtfilepath = dialog.FileName;
			}
		}

		private void SetingSelect_Click(object sender, EventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog()
			{
				Filter = "Test Setting File (*.testsettings)|*.testsettings",
				FilterIndex = 1
			};
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				this.TestSettings.Text = dialog.FileName;
			}
		}

		private void ShowTestData(DataTable dt)
		{
			DataGridViewCheckBoxColumn chk = new DataGridViewCheckBoxColumn()
			{
				Name = this.rExecute
			};
			this.TestMethodGrid.Columns.Add(chk);
			this.TestMethodGrid.AutoResizeColumns();
			this.TestMethodGrid.DataSource = dt;
			this.TestMethodGrid.Columns[this.rTestName].ReadOnly = true;
			this.TestMethodGrid.Columns[this.rTestGUID].Visible = false;
			this.TestMethodGrid.Columns[this.rTestPriority].ReadOnly = true;
			this.TestMethodGrid.Columns[this.rTestOwner].ReadOnly = true;
			this.TestMethodGrid.Columns[this.rTestCategory].ReadOnly = true;
			foreach (DataGridViewRow row in (IEnumerable)this.TestMethodGrid.Rows)
			{
				(row.Cells[this.rExecute] as DataGridViewCheckBoxCell).Value = true;
			}
		}

		private void TestLinkNode(XmlDocument parent, DataGridViewRow row, ref XmlNode note)
		{
			XmlAttribute t_id = parent.CreateAttribute("id");
			t_id.Value = row.Cells[this.rTestGUID].Value.ToString();
			XmlAttribute t_name = parent.CreateAttribute("name");
			t_name.Value = row.Cells[this.rTestName].Value.ToString();
			XmlAttribute t_storage = parent.CreateAttribute("storage");
			t_storage.Value = this.DLLFileText.Text;
			XmlAttribute t_type = parent.CreateAttribute("type");
			t_type.Value = string.Concat("Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestElement, Microsoft.VisualStudio.QualityTools.Tips.UnitTest.ObjectModel, ", "Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
			note.Attributes.Append(t_id);
			note.Attributes.Append(t_name);
			note.Attributes.Append(t_storage);
			note.Attributes.Append(t_type);
		}

		private DataTable TestMethodData()
		{
			DataTable dt = new DataTable();
			dt.Columns.Add(this.rTestName);
			dt.Columns.Add(this.rTestGUID);
			dt.Columns.Add(this.rTestPriority);
			dt.Columns.Add(this.rTestOwner);
			dt.Columns.Add(this.rTestCategory);
			return dt;
		}

		private void TestMethodGrid_Sorted(object sender, EventArgs e)
		{
			if (!this.SelectAll.Checked)
			{
				foreach (DataGridViewRow row in (IEnumerable)this.TestMethodGrid.Rows)
				{
					(row.Cells[this.rExecute] as DataGridViewCheckBoxCell).Value = false;
				}
			}
			else
			{
				foreach (DataGridViewRow row in (IEnumerable)this.TestMethodGrid.Rows)
				{
					(row.Cells[this.rExecute] as DataGridViewCheckBoxCell).Value = true;
				}
			}
		}

		private void TestProperty_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (this.TestProperty.SelectedItem.ToString() == "All")
			{
				this.PropertyValue.Enabled = false;
			}
			else
			{
				this.PropertyValue.Enabled = true;
			}
			if (this.TestProperty.SelectedItem.ToString() != "Text")
			{
				this.PropertyValue.Visible = true;
				this.SelectTxt.Visible = false;
			}
			else
			{
				this.PropertyValue.Visible = false;
				this.SelectTxt.Visible = true;
			}
			this.PropertyValue.Text = "";
			this.TestMethodGrid.DataSource = null;
			this.TestMethodGrid.Rows.Clear();
			this.TestMethodGrid.Columns.Clear();
			this.SelectAll.Enabled = false;
			this.UnselectAll.Enabled = false;
			this.SelectAll.Checked = true;
		}

		private void UnselectAll_CheckedChanged(object sender, EventArgs e)
		{
			foreach (DataGridViewRow row in (IEnumerable)this.TestMethodGrid.Rows)
			{
				(row.Cells[this.rExecute] as DataGridViewCheckBoxCell).Value = false;
			}
		}

        private void DLLFileText_TextChanged(object sender, EventArgs e)
        {

        }

        private void TestMethodGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}