using HRMS.handler;
using System;
using System.Net;
using System.Threading;
using HRMS.models;
using System.Configuration;

namespace HRMS.httpServer
{
    public sealed class HttpServer
    {
        private static int activeServerCount;
        private readonly HttpListener listener;
        private readonly TimeKeepingHandler timeKeepingHandler;
        private readonly Func<TimeKeepingRequest, AttendanceResult> callback;
        private Thread thread;
        private volatile bool isStopping;
        private int ownsActiveSlot;

        public static bool IsRunning
        {
            get { return Volatile.Read(ref activeServerCount) != 0; }
        }

        public HttpServer(Func<TimeKeepingRequest, AttendanceResult> callback)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            listener = new HttpListener();
            listener.Prefixes.Add("http://+:" + GetPort() + "/timekeeping/");
            timeKeepingHandler = new TimeKeepingHandler();
            this.callback = callback;
        }

        public void Start()
        {
            if (listener.IsListening)
                return;

            if (Interlocked.CompareExchange(ref activeServerCount, 1, 0) != 0)
                throw new InvalidOperationException("An attendance server is already running.");

            try
            {
                isStopping = false;
                listener.Start();
                Interlocked.Exchange(ref ownsActiveSlot, 1);
                thread = new Thread(ListenLoop);
                thread.IsBackground = true;
                thread.Start();
            }
            catch
            {
                try
                {
                    if (listener.IsListening)
                        listener.Stop();
                }
                catch (HttpListenerException)
                {
                }
                Interlocked.Exchange(ref activeServerCount, 0);
                throw;
            }
        }

        private void ListenLoop()
        {
            try
            {
                while (!isStopping && listener.IsListening)
                {
                    var context = listener.GetContext();
                    var request = context.Request;
                    var response = context.Response;
                    var path = request.Url == null ? String.Empty : request.Url.AbsolutePath.TrimEnd('/');

                    if (!String.Equals(path, "/timekeeping", StringComparison.OrdinalIgnoreCase))
                    {
                        response.StatusCode = 404;
                        response.Close();
                        continue;
                    }

                    timeKeepingHandler.Handle(request, response, callback);
                }
            }
            catch (HttpListenerException)
            {
                if (!isStopping)
                    Console.Error.WriteLine("The timekeeping HTTP listener stopped unexpectedly.");
            }
            catch (ObjectDisposedException)
            {
                if (!isStopping)
                    Console.Error.WriteLine("The timekeeping HTTP listener was disposed unexpectedly.");
            }
            catch (Exception ex)
            {
                if (!isStopping)
                    Console.Error.WriteLine("Unexpected attendance listener error: " + ex);
            }
            finally
            {
                ReleaseActiveSlot();
            }
        }

        public void Close()
        {
            isStopping = true;
            if (listener.IsListening)
                listener.Stop();
            listener.Close();

            if (thread != null && thread.IsAlive)
                thread.Join(1000);
            ReleaseActiveSlot();
        }

        private void ReleaseActiveSlot()
        {
            if (Interlocked.Exchange(ref ownsActiveSlot, 0) != 0)
                Interlocked.Exchange(ref activeServerCount, 0);
        }

        public static int GetPort()
        {
            int port;
            var setting = ConfigurationManager.AppSettings["AttendanceApiPort"];
            return Int32.TryParse(setting, out port) && port >= 1024 && port <= 65535 ? port : 8080;
        }
    }
}
