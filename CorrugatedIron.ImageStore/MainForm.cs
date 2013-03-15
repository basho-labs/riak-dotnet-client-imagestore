using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CorrugatedIron.Models;
using ImageResizer;

namespace CorrugatedIron.ImageStore
{
    public partial class MainForm : Form
    {
        private const string ImageFilter = @"Image Files|*.jpg;*.jpeg;*.tiff;*.gif;*.png";

        private readonly IRiakEndPoint _cluster;
        private readonly IRiakClient _client;
        private readonly BackgroundWorker _worker;
        private readonly ResizeSettings _resizeSettings;

        private string _bucket = "ci_images";

        public MainForm()
        {
            InitializeComponent();

            _cluster = RiakCluster.FromConfig("riakConfig");
            _client = _cluster.CreateClient();

            _worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = false
            };

            _worker.DoWork += AsyncLoadImages;

            _resizeSettings = new ResizeSettings
            {
                MaxWidth = 120,
                MaxHeight = 100,
                Format = "jpg"
            };
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            txtBucketName.Text = _bucket;
            btnChangeBucket.PerformClick();
            btnAddImage.Enabled = false;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _cluster.Dispose();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                AddExtension = false,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = ImageFilter,
                Multiselect = false
            };

            var result = dlg.ShowDialog();

            if (result == DialogResult.OK)
            {
                txtImage.Text = dlg.FileName;
                btnAddImage.Enabled = true;
            }
        }

        private void btnAddImage_Click(object sender, EventArgs e)
        {
            var fileName = txtImage.Text;
            var contentType = GetMimeType(fileName);
            var key = Path.GetFileName(fileName);
            var content = File.ReadAllBytes(fileName);
            var riakObj = new RiakObject(_bucket, key, content, contentType, null);

            var result = _client.Put(riakObj);

            if (result.IsSuccess)
            {
                AppendImage(riakObj);
                MessageBox.Show(this, "Image was added successfully.", "Succeeded!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, "Error adding image: " + result.ErrorMessage, "Failed!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetMimeType(string fileName)
        {
            var mimeType = "application/unknown";
            var ext = Path.GetExtension(fileName).ToLower();
            var regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);

            if (regKey != null && regKey.GetValue("Content Type") != null)
            {
                mimeType = regKey.GetValue("Content Type").ToString();
            }

            return mimeType;
        }

        private void btnChangeBucket_Click(object sender, EventArgs e)
        {
            if (txtBucketName.Text.Trim().Length == 0)
            {
                MessageBox.Show(this, "Please specify a bucket name.", "Bucket name needed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            imgPanel.Controls.Clear();
            _worker.RunWorkerAsync(_bucket);
        }

        private void AsyncLoadImages(object sender, DoWorkEventArgs e)
        {
            var bucket = (string)e.Argument;
            var keysResult = _client.ListKeysFromIndex(bucket);

            if (!keysResult.IsSuccess)
            {
                // TODO:
                return;
            }

            foreach (var key in keysResult.Value)
            {
                _client.Async.Get(bucket, key).ContinueWith(t =>
                    {
                        if (t.Result.IsSuccess)
                        {
                            AppendImage(t.Result.Value);
                        }
                    });
            }
        }

        private void AppendImage(RiakObject image)
        {
            using (var inStream = new MemoryStream(image.Value))
            using (var outStream = new MemoryStream())
            {
                ImageBuilder.Current.Build(inStream, outStream, _resizeSettings);

                var picture = new PictureBox
                {
                    Image = Image.FromStream(outStream),
                    Size = new Size(_resizeSettings.MaxWidth, _resizeSettings.MaxHeight)
                };

                if (imgPanel.InvokeRequired)
                {
                    imgPanel.Invoke(new MethodInvoker(() => imgPanel.Controls.Add(picture)));
                }
                else
                {
                    imgPanel.Controls.Add(picture);
                }
            }
        }
    }
}
