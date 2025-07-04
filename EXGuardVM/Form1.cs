using dnlib.DotNet;
using EXGuard.Internal;
using EXGuardVM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EXGuardVM
{
    public partial class Form1 : Form
    {
        public ModuleDefMD module = null;

        public MethodTreeLoader TreeViewMethodManager = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        #region " UI "

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string FileName = openFileDialog1.FileName;
                    if (File.Exists(FileName))
                    {
                        ResetUI();

                        if (module != null)
                        {
                            module.Dispose();
                        }

                        if (TreeViewMethodManager != null)
                        {
                            GC.SuppressFinalize(TreeViewMethodManager);
                            TreeViewMethodManager = null;
                        }

                        module = ModuleDefMD.Load(FileName);

                        TreeView treeView = new TreeView();
                        treeView.BackColor = panel1.BackColor;
                        treeView.ForeColor = panel1.ForeColor;
                        panel1.Controls.Add(treeView);
                        treeView.Dock = DockStyle.Fill;

                        TreeViewMethodManager = new MethodTreeLoader(treeView, module);
                        TreeViewMethodManager.ExcludeCompilerGenerated = true;
                        TreeViewMethodManager.HighlightUserStaticMethods = true;

                        var progress = new Progress<MethodTreeLoader.ProgressEventArgs>(args =>
                        {
                            this.Text = $"EXGuardVM - {args.CurrentOperation} ({args.PercentComplete}%)";
                        });

                        var thread = new Thread(async () =>
                        {
                            await TreeViewMethodManager.LoadMethodsAsync(progress);
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                button2.Visible = true;
                                treeView.Visible = true;
                            });
                        });
                        thread.Priority = ThreadPriority.Highest;
                        thread.Start();

                        checkBox4.Checked = TreeViewMethodManager.All;
                        checkBox1.Checked = TreeViewMethodManager.ExcludeConstructors;
                        checkBox2.Checked = TreeViewMethodManager.ExcludeRedMethods;
                        checkBox3.Checked = TreeViewMethodManager.ExcludeUnsafeMethods;

                        label1.Text = module.Assembly.FullName;
                        textBox1.Text = FileName;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            label1.Text = "Virtualizing Methods...";
            Task<bool> Protect = ProtectAssembly();

            Protect.GetAwaiter().OnCompleted(() =>
            {
                if (Protect.Result)
                {
                    label1.Text = "Assembly protected successfully";
                }
                else
                {
                    label1.Text = "Failed to protect assembly";
                }
                button2.Enabled = true;
            });
        }

        private void ResetUI()
        {
            label1.Text = "";
            textBox1.Text = "";
            panel1.Controls.Clear();
        }

        #endregion " UI "

        #region " Protect "

        private async Task<bool> ProtectAssembly()
        {
            try
            {
                if (module == null) throw new Exception("Assembly not loaded");
                if (TreeViewMethodManager == null) throw new Exception("MethodsLoader not initialized");

                string output = Path.Combine(Path.GetDirectoryName(module.Location), Path.GetFileNameWithoutExtension(module.Location) + "_VM" + Path.GetExtension(module.Location));
                string RuntimeVM_Name = "EXGuard.Runtime.dll";
                List<MethodDef> SelectedMethods = MethodTreeLoader.ResolveMethodsFromTokens(module, TreeViewMethodManager.GetSelectedMethodTokens());
                HashSet<MethodDef> methodSet = new HashSet<MethodDef>(SelectedMethods);
                methodSet.Distinct();

                new EXGuardTask().Exceute(module, methodSet, output, RuntimeVM_Name, "", "");

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        #endregion " Protect "

        #region " MethodsLoader Settings "

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (module != null && TreeViewMethodManager != null)
                TreeViewMethodManager.All = checkBox4.Checked;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (module != null && TreeViewMethodManager != null)
                TreeViewMethodManager.ExcludeConstructors = checkBox1.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (module != null && TreeViewMethodManager != null)
                TreeViewMethodManager.ExcludeRedMethods = checkBox2.Checked;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (module != null && TreeViewMethodManager != null)
                TreeViewMethodManager.ExcludeUnsafeMethods = checkBox3.Checked;
        }

        #endregion " MethodsLoader Settings "
    }
}