using DevExpress.Xpf.Core;
using HRMS.httpServer;
using HRMS.models;
using HRMS.utils;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using HRMS.services;
using System.Data.Entity;

namespace HRMS.windows
{
    /// <summary>
    /// Interaction logic for AttendanceWindow.xaml
    /// </summary>
    public partial class TimekeepingWindow : ThemedWindow

    {
        private Employee employee;
        private readonly HUMAN_MANAGEMENTEntities hrmsContext;
        private readonly HttpServer httpServer;
        private Timekeeping timekeeping;
        private readonly Action<int> callback;

        public TimekeepingWindow(HUMAN_MANAGEMENTEntities hrms, Action<int> callback)
        {
            InitializeComponent();
            this.Resources.MergedDictionaries.Add(DictUtil.getCurrent());
            hrmsContext = hrms;
            this.callback = callback;
            httpServer = new HttpServer(updateEmployee);
            httpServer.Start();
        }

        private AttendanceResult updateEmployee(TimeKeepingRequest request)
        {
            var result = new AttendanceService().Record(new AttendanceInput
            {
                EmployeeId = request.EmployeeId > 0 ? (int?)request.EmployeeId : null,
                EmployeeCode = request.EmployeeCode,
                CardUid = request.CardUid,
                // Dùng đồng hồ của máy chủ cho lượt quét trực tiếp để người dùng không
                // thể thay đổi giờ điện thoại và làm sai dữ liệu công.
                EventTime = DateTime.Now,
                ExternalEventId = request.ExternalEventId,
                Source = "client-api",
                DeviceName = (request.DeviceName ?? "Mobile client")
                    + (String.IsNullOrWhiteSpace(request.ClientAddress) ? String.Empty : " @ " + request.ClientAddress),
                RawData = "clientEventTime="
                    + (request.EventTime.HasValue ? request.EventTime.Value.ToString("o") : "(missing)")
                    + "; clientAddress=" + (request.ClientAddress ?? "(unknown)")
            });
            var application = Application.Current;
            if (application != null)
                application.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (IsLoaded)
                        DisplayAttendanceResult(result);
                }));
            return result;
        }

        private void DisplayAttendanceResult(AttendanceResult result)
        {
            reset();
            resultMessage.Text = result.Message;
            if (!result.Success)
                return;

            employee = hrmsContext.Employees.SingleOrDefault(item => item.id == result.EmployeeId);
            if (employee == null)
                return;

            this.DataContext = employee;
            if (employee.image != null)
                MyImage.Source = ImageUtil.loadImage(employee.image);
            timekeeping = hrmsContext.Timekeepings.AsNoTracking()
                .FirstOrDefault(t => t.employeeId == employee.id && t.workDate == DateTime.Today);
            if (timekeeping != null)
            {
                arriveTime.Text = timekeeping.arriveTime.ToString(@"hh\:mm\:ss");
                if (timekeeping.leaveTime.TotalSeconds == 0)
                {
                    leaveTimeLabel.FontWeight = FontWeights.Bold;
                    leaveTime.Text = DateTime.Now.ToString("HH:mm:ss");
                }
                else
                {
                    leaveTime.Text = timekeeping.leaveTime.ToString(@"hh\:mm\:ss");
                }
            }
            else
            {
                arrriveTimeLabel.FontWeight = FontWeights.Bold;
                arriveTime.Text = DateTime.Now.ToString("HH:mm:ss");
            }

            if (callback != null)
                callback(0);
        }

        private void window_Closed(object sender, EventArgs e)
        {
            httpServer.Close();
            HRMS.holders.TokenHolder.Clear();
        }

        private void cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            reset();
        }

        private void reset()
        {
            this.DataContext = null;
            employee = null;
            arriveTime.Text = null;
            leaveTime.Text = null;
            MyImage.Source = null;
            timekeeping = null;
            arrriveTimeLabel.FontWeight = FontWeights.Normal;
            leaveTimeLabel.FontWeight = FontWeights.Normal;
        }
    }
}
