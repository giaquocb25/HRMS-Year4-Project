using DevExpress.Xpf.Core;
using DevExpress.Xpf.Printing;
using HRMS.models;
using HRMS.services;
using HRMS.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for PayslipWindow.xaml
    /// </summary>
    public partial class PayslipWindow : ThemedWindow
    {
        List<PayslipItem> Data;
        public PayslipWindow(Payroll p)
        {
            if (!AuthorizationService.Can(Permission.ViewPayroll))
                throw new UnauthorizedAccessException("Tài khoản không có quyền xem phiếu lương.");
            if (p == null)
                throw new ArgumentNullException("p");

            InitializeComponent();
            loadData(p);
        }
        void loadData(Payroll payroll)
        {

            Employee employee;
            using (var hrms = new HUMAN_MANAGEMENTEntities())
                employee = hrms.Employees.AsNoTracking().FirstOrDefault(e => e.id == payroll.employeeId);

            if (employee == null)
                throw new InvalidOperationException("Employee for this payroll was not found.");

            var items = new List<PayslipItem>();
            var dict = DictUtil.getCurrent();
            items.Add(new PayslipItem(dict["name"].ToString(), employee.name));
            items.Add(new PayslipItem(dict["standardWorkingDays"].ToString(), TimeUtil.StandardWorkingDays));
            items.Add(new PayslipItem(dict["actualWorkingDays"].ToString(), payroll.workdays));
            items.Add(new PayslipItem(dict["grossSalary"].ToString(), employee.salary.ToString("#,###")));
            items.Add(new PayslipItem(dict["derivedSalary"].ToString(), payroll.amount.ToString("#,###")));
            var insuranceRate = TimeUtil.MandatoryInsuranceRate;
            var insurance = Decimal.Round(payroll.amount * insuranceRate,
                0, MidpointRounding.AwayFromZero);
            items.Add(new PayslipItem(dict["mandatoryInsurance"] + " (" + insuranceRate.ToString("P1") + ")",
                insurance.ToString("#,###")));
            items.Add(new PayslipItem(dict["netIncome"].ToString(), (payroll.amount - insurance).ToString("#,###")));
            Data = items;
            SimpleLink payslipLink = new SimpleLink();
            payslipLink.ReportHeaderTemplate = (DataTemplate)Resources["HeaderTemplate"];
            payslipLink.DetailTemplate = (DataTemplate)Resources["Payslip"];
            payslipLink.DetailCount = Data.Count;
            payslipLink.ReportHeaderData = Thread.CurrentThread.CurrentCulture.ToString().Equals("vi-VN") ? "Bảng lương tháng " + payroll.month : "PAYSLIP";
            preview.DocumentSource = payslipLink;
            payslipLink.CreateDetail += link_CreateDetail;
            payslipLink.CreateDocument(true);
        }

        void link_CreateDetail(object sender, CreateAreaEventArgs e)
        {
            e.Data = Data[e.DetailIndex];
        }
    }
}
