using DevExpress.Xpf.Core;
using System;
using System.Data.Entity;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Linq;
using HRMS.ViewModels;
using HRMS.utils;

namespace HRMS.windows
{
    /// <summary>
    /// Interaction logic for ImageWindow.xaml
    /// </summary>
    public partial class ImageWindow : ThemedWindow
    {
        private HUMAN_MANAGEMENTEntities hrms;
        private byte[] imageBytes = null;
        private Employee employee;

        public ImageWindow(Employee e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            InitializeComponent();
            this.Resources.MergedDictionaries.Add(DictUtil.getCurrent());
            hrms = new HUMAN_MANAGEMENTEntities();
            this.employee = e;
            this.DataContext = employee;
            MyImage.Source = ImageUtil.loadImage(employee.image);
        }

        private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = false,
                Filter = "Images (*.jpg,*.png)|*.jpg;*.png|All Files(*.*)|*.*"
            };


            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) { return; }

            var fileInfo = new FileInfo(dialog.FileName);
            if (fileInfo.Length > 5 * 1024 * 1024)
            {
                ThemedMessageBox.Show(
                    title: "Ảnh quá lớn",
                    text: "Vui lòng chọn ảnh JPG hoặc PNG nhỏ hơn 5 MB.",
                    messageBoxButtons: MessageBoxButton.OK,
                    icon: MessageBoxImage.Warning);
                return;
            }

            ImagePath.Text = dialog.FileName;
            imageBytes = File.ReadAllBytes(dialog.FileName);
            MyImage.Source = ImageUtil.loadImage(imageBytes);
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(ImagePath.Text))
            {
                employee.image = imageBytes;
                hrms.Employees.Where(item => item.id == employee.id)
                    .ToList()
                    .ForEach(item => item.image = imageBytes);
                hrms.SaveChanges();
                ThemedMessageBox.Show(
                    title: "Đã lưu ảnh",
                    text: "Ảnh nhân viên đã được cập nhật.",
                    messageBoxButtons: MessageBoxButton.OK,
                    icon: MessageBoxImage.Information);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (hrms != null)
                hrms.Dispose();
            base.OnClosed(e);
        }
    }
}
