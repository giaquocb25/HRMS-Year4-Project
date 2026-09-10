using DevExpress.Xpf.Core;
using DevExpress.XtraPrinting.BarCode;
using HRMS.holders;
using HRMS.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace HRMS.windows
{
    /// <summary>
    /// Interaction logic for TokenWindow.xaml
    /// </summary>
    public partial class QRWindow : ThemedWindow
    {
        public QRWindow(int id)
        {
            InitializeComponent();
            this.Resources.MergedDictionaries.Add(DictUtil.getCurrent());
            qr.EditValue = id;
        }
    }
}
