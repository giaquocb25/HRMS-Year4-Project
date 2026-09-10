using HRMS.holders;
using HRMS.models;
using HRMS.windows;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace HRMS.handler
{
    public class TimeKeepingHandler
    {

        public void Handle(HttpListenerRequest request, HttpListenerResponse response, Func<TimeKeepingRequest, AttendanceResult> callback)
        {
            try
            {
                if (String.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    WriteJson(response, 200, new
                    {
                        success = true,
                        service = "HRMS attendance",
                        serverTime = DateTimeOffset.Now,
                        tokenRequired = true
                    });
                    return;
                }

                if (!String.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    WriteJson(response, 405, new { success = false, message = "Only POST is supported." });
                    return;
                }

                if (request.ContentLength64 > 4096)
                {
                    WriteJson(response, 413, new { success = false, message = "Request body is too large." });
                    return;
                }

                string requestBody;
                using (var body = new MemoryStream())
                {
                    var buffer = new byte[1024];
                    int read;
                    while ((read = request.InputStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        body.Write(buffer, 0, read);
                        if (body.Length > 4096)
                        {
                            WriteJson(response, 413, new { success = false, message = "Request body is too large." });
                            return;
                        }
                    }
                    requestBody = (request.ContentEncoding ?? Encoding.UTF8).GetString(body.ToArray());
                }

                var timeKeepingRequest = JsonConvert.DeserializeObject<TimeKeepingRequest>(requestBody);
                if (timeKeepingRequest == null || (timeKeepingRequest.EmployeeId <= 0
                    && String.IsNullOrWhiteSpace(timeKeepingRequest.EmployeeCode)
                    && String.IsNullOrWhiteSpace(timeKeepingRequest.CardUid)))
                {
                    WriteJson(response, 400, new { success = false, message = "Employee id, employee code or card UID is required." });
                    return;
                }

                if (!TokenHolder.IsValid(timeKeepingRequest.Token))
                {
                    WriteJson(response, 401, new { success = false, message = "The setup token is invalid or expired." });
                    return;
                }

                timeKeepingRequest.ClientAddress = request.RemoteEndPoint == null
                    ? null
                    : request.RemoteEndPoint.Address.ToString();

                var result = callback(timeKeepingRequest);

                if (result == null || !result.Success)
                {
                    WriteJson(response, 422, new { success = false, message = result == null ? "Attendance failed." : result.Message });
                    return;
                }

                WriteJson(response, 200, new
                {
                    success = true,
                    duplicate = result.Duplicate,
                    employeeId = result.EmployeeId,
                    employeeName = result.EmployeeName,
                    message = result.Message
                });

            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error while handling timekeeping request: " + e);
                if (response.OutputStream.CanWrite)
                    WriteJson(response, 400, new { success = false, message = "Invalid request." });
            }
        }

        private static void WriteJson(HttpListenerResponse response, int statusCode, object body)
        {
            var responseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body));
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = responseBytes.Length;
            response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
            response.Close();
        }
    }

}
