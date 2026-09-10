using DevExpress.Xpf.Core;
using HRMS.utils;
using HRMS.ViewModels;


namespace HRMS.windows
{
    /// <summary>
    /// Interaction logic for PayrollWindow.xaml
    /// </summary>
    public partial class PayrollWindow : ThemedWindow
    {
        public PayrollWindow(Employee e)
        {
            if (e == null)
                throw new System.ArgumentNullException("e");

            InitializeComponent();
            this.Resources.MergedDictionaries.Add(DictUtil.getCurrent());
            label.Content = e.name;
            gridControl.FilterString = "employeeId == " + e.id;
            this.DataContext = new PayrollViewModel(e.id);
        }

        protected override void OnClosed(System.EventArgs e)
        {
            var viewModel = DataContext as PayrollViewModel;
            if (viewModel != null)
                viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}
