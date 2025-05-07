using System;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic; // Add this namespace for List<T>
using System.Linq; // Add this namespace for LINQ methods like Cast
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace FileOrganizerApp
{
    public partial class MainForm : Form
    {
        private string selectedFolderPath;
        private BackgroundWorker backgroundWorker;

        public MainForm()
        {
            InitializeComponent();
            InitializeBackgroundWorker();
        }

        private void InitializeBackgroundWorker()
        {
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedFolderPath = folderDialog.SelectedPath;
                    txtSelectedFolder.Text = selectedFolderPath;
                    LoadFolderContents();
                }
            }
        }

        private void LoadFolderContents()
        {
            lvFiles.Items.Clear();

            if (!Directory.Exists(selectedFolderPath))
            {
                MessageBox.Show("Selected folder does not exist!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var files = Directory.GetFiles(selectedFolderPath);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var item = new ListViewItem(fileName);
                item.SubItems.Add(fileName); // Second column for editable name
                item.Tag = file; // Store full path in Tag
                lvFiles.Items.Add(item);
            }
        }

        private void btnOrganize_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFolderPath))
            {
                MessageBox.Show("Please select a folder first!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (lvFiles.Items.Count == 0)
            {
                MessageBox.Show("No files to organize!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Update any renamed files first
            foreach (ListViewItem item in lvFiles.Items)
            {
                string originalPath = (string)item.Tag;
                string newName = item.SubItems[1].Text;
                string originalName = Path.GetFileName(originalPath);

                if (newName != originalName)
                {
                    try
                    {
                        string newPath = Path.Combine(selectedFolderPath, newName);
                        File.Move(originalPath, newPath);
                        item.Tag = newPath; // Update the stored path
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error renaming {originalName} to {newName}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            txtConsole.Clear();
            progressBar.Value = 0;
            btnOrganize.Enabled = false;
            btnSelectFolder.Enabled = false;

            backgroundWorker.RunWorkerAsync();
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            int totalFiles = lvFiles.Items.Count;
            int processedFiles = 0;

            // Retrieve the items from the ListView
            var items = (List<ListViewItem>)lvFiles.Invoke(new Func<List<ListViewItem>>(() => lvFiles.Items.Cast<ListViewItem>().ToList()));

            foreach (var item in items) // Add a foreach loop to iterate over the items
            {
                string filePath = (string)item.Tag;
                string fileName = Path.GetFileName(filePath);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

                // Skip the organize executable/script itself
                if (fileNameWithoutExtension.Equals("organize", StringComparison.OrdinalIgnoreCase))
                {
                    processedFiles++;
                    continue;
                }

                try
                {
                    string folderPath = Path.Combine(selectedFolderPath, fileNameWithoutExtension);

                    // Report progress before creating folder
                    backgroundWorker.ReportProgress((processedFiles * 100) / totalFiles, $"Creating folder: {fileNameWithoutExtension}");

                    Directory.CreateDirectory(folderPath);

                    // Report progress before moving
                    backgroundWorker.ReportProgress((processedFiles * 100) / totalFiles, $"Moving {fileName} to {fileNameWithoutExtension}");

                    string newFilePath = Path.Combine(folderPath, fileName);
                    File.Move(filePath, newFilePath);

                    processedFiles++;
                    backgroundWorker.ReportProgress((processedFiles * 100) / totalFiles, $"Success: {fileName} moved to {fileNameWithoutExtension}");
                }
                catch (Exception ex)
                {
                    backgroundWorker.ReportProgress((processedFiles * 100) / totalFiles, $"Error: {ex.Message}");
                }
            }
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)        {
            progressBar.Value = e.ProgressPercentage;
            if (e.UserState != null)
            {
                txtConsole.AppendText(e.UserState.ToString() + Environment.NewLine);
            }
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Value = 100;
            btnOrganize.Enabled = true;
            btnSelectFolder.Enabled = true;
            LoadFolderContents(); // Refresh the file list
            txtConsole.AppendText("Organization complete!" + Environment.NewLine);
        }

        private void lvFiles_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (e.Label != null && e.Label.Length > 0)
            {
                lvFiles.Items[e.Item].SubItems[1].Text = e.Label;
            }
            else
            {
                e.CancelEdit = true;
            }
        }

        private void lvFiles_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}