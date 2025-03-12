using dnlib.DotNet;
using EXGuard.Internal;
using EXGuardVM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EXGuardVM
{
    public partial class Form1 : Form
    {
        public ModuleDefMD module = null;

        public MethodTreeLoader MethodsLoader = null;

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

                        if (MethodsLoader != null)
                        {
                            GC.SuppressFinalize(MethodsLoader);
                        }

                        module = ModuleDefMD.Load(FileName);

                        TreeView treeView = new TreeView();
                        treeView.BackColor = panel1.BackColor;
                        treeView.ForeColor = panel1.ForeColor;
                        panel1.Controls.Add(treeView);
                        treeView.Dock = DockStyle.Fill;

                        MethodsLoader = new MethodTreeLoader(treeView, module);
                        MethodsLoader.LoadMethods();
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

        #endregion

        #region " Protect "

        private async Task<bool> ProtectAssembly()
        {
            try
            {
                if (module == null) throw new Exception("Assembly not loaded");
                if (MethodsLoader == null) throw new Exception("MethodsLoader not initialized");

                string output = Path.Combine(Path.GetDirectoryName(module.Location), Path.GetFileNameWithoutExtension(module.Location) + "_VM" + Path.GetExtension(module.Location));
                string RuntimeVM_Name = "EXGuard.Runtime.dll";
                List<MethodDef> SelectedMethods = MethodTreeLoader.ResolveMethodsFromTokens(module, MethodsLoader.GetSelectedMethodTokens());
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

        #endregion

        #region " MethodsLoader Settings "

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (module != null && MethodsLoader != null)
                MethodsLoader.All = checkBox4.Checked;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (module != null && MethodsLoader != null)
                MethodsLoader.ExcludeConstructors = checkBox1.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (module != null && MethodsLoader != null)
                MethodsLoader.ExcludeRedMethods = checkBox2.Checked;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (module != null && MethodsLoader != null)
                MethodsLoader.ExcludeUnsafeMethods = checkBox3.Checked;
        }

        #endregion

    }
}
