using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SyncSVNTestForms
{
    class SyncSVNTestForm : Form
    {
        private ListView listView1;
        private string message;
        public CheckedListBox checkedListBox1;
        private List<string> conflictFiles;
        public Button button1;
        private Label label1;
        private SortedDictionary<string, bool> conflictResolve;

        public SyncSVNTestForm() : this("Message", new List<string>() { "abcd.txt" }) 
        {
            if (message == null)
                message = "Message";
            if (conflictFiles == null)
                conflictFiles = new List<string>() { "abcd.txt" };
        }

        public SyncSVNTestForm(string msg, List<string> files)
        {
            message = msg;
            conflictFiles = files;
            conflictResolve = new SortedDictionary<string, bool>();
            foreach (var file in conflictFiles)
                conflictResolve.Add(file, true);

            InitializeComponent();

            var objects = new System.Windows.Forms.ListBox.ObjectCollection(checkedListBox1);
            foreach (var file in conflictFiles)
                objects.Add(file);

            this.checkedListBox1.Items.AddRange(objects);
            label1.Text = msg;
            label1.MaximumSize = new System.Drawing.Size(300, 0);
            label1.AutoSize = true;
        }

        private void InitializeComponent()
        {
            this.checkedListBox1 = new System.Windows.Forms.CheckedListBox();
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // checkedListBox1
            // 
            this.checkedListBox1.FormattingEnabled = true;
            this.checkedListBox1.Location = new System.Drawing.Point(12, 81);
            this.checkedListBox1.Name = "checkedListBox1";
            this.checkedListBox1.Size = new System.Drawing.Size(249, 139);
            this.checkedListBox1.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(79, 226);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(90, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Подтвердить";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(23, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "label1";
            // 
            // SyncSVNTestForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.checkedListBox1);
            this.Name = "SyncSVNTestForm";
            this.Load += new System.EventHandler(this.SyncSVNTestForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            List<string> checkedItems = new List<string>();
            foreach (var item in checkedListBox1.CheckedItems)
                checkedItems.Add(item.ToString());

            if (e.NewValue == CheckState.Checked)
                checkedItems.Add(checkedListBox1.Items[e.Index].ToString());
            else
                checkedItems.Remove(checkedListBox1.Items[e.Index].ToString());

            foreach (string item in checkedItems) {
                conflictResolve[item] = true;
            }
        }

        private void SyncSVNTestForm_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
