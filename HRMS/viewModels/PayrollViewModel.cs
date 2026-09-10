using DevExpress.Mvvm;
using System.Collections.ObjectModel;
using HRMS.services;
using System.Data.Entity;
using System.Linq;

namespace HRMS.ViewModels
{
    public class PayrollViewModel : ViewModelBase, System.IDisposable
    {
        private readonly HUMAN_MANAGEMENTEntities hrmsContext;
        public PayrollViewModel(int id)
        {
            if (!AuthorizationService.Can(Permission.ViewPayroll))
                throw new System.UnauthorizedAccessException("Tài khoản không có quyền xem bảng lương.");

            hrmsContext = new HUMAN_MANAGEMENTEntities();

            hrmsContext.Payrolls.Where(p => p.employeeId == id).Load();
            Payrolls = hrmsContext.Payrolls.Local;
        }
        public ObservableCollection<Payroll> Payrolls
        {
            get { return GetValue<ObservableCollection<Payroll>>(); }
            set { SetValue(value); }
        }

        public void Dispose()
        {
            hrmsContext.Dispose();
        }
    }
}
