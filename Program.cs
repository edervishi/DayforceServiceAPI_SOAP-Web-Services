
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using DayforceServiceSampleClient.DayforceService;

namespace DayforceServiceSampleClientSOAP
{
    class Program
    {
        private const string clientName = "winn";
        private const string userName = "edervishi@winnco.com";  // this user must have the web services feature enabled.
        private const string password = "";

        public static void Main(string[] args)
        {
            SampleCallToGetEmployees();
            SampleCallToGetEmployeeXRefCodes();
            SampleCallForReports();
            SampleCallForReportPages();
            SampleCallToGetNotificationEvents();
            SampleCallToGetNotificationData();

            Console.WriteLine("Press Enter to terminate this application.");
            Console.ReadLine();
        }

        private static void SampleCallToGetEmployees()
        {
            DayforceServiceClient dayforceServiceClient;
            string sessionTicket;

            InitiateSession(clientName, userName, password, out dayforceServiceClient, out sessionTicket);

            if (dayforceServiceClient == null || sessionTicket == null)
                return;

            // Sample showing a request for employees after a particular date
            DFRequest dfRequest = new GetEmployeesRequest()
            {
                FilterHireStartDate = new DateTime(2010, 1, 1),
                IncludeSubordinateObjects = true
            };

            DFResponseList dfResponseList = dayforceServiceClient.Query(sessionTicket, dfRequest);

            if (dfResponseList.Error.Code == 0)
            {
                foreach (Employee employee in dfResponseList.Result)
                {
                    OutputThePropertyValues(employee, "");
                    //Console.WriteLine(employee.FirstName + " " + employee.LastName + " (" + employee.EmployeeNumber + ")");
                }
            }
            else
            {
                Console.WriteLine(dfResponseList.Error.Code + " - " + dfResponseList.Error.Message);
            }

            // Logout
            dayforceServiceClient.Logout(sessionTicket);
        }

        private static void SampleCallToGetNotificationEvents()
        {
            DayforceServiceClient dayforceServiceClient;
            string sessionTicket;

            InitiateSession(clientName, userName, password, out dayforceServiceClient, out sessionTicket);

            if (dayforceServiceClient == null || sessionTicket == null)
                return;

            // Sample showing a request for employees after a particular date
            DFRequest dfRequest = new DFNotificationsRequest()
            {
                // The SubscriberReferenceCode indicates which subscription to pull events for.
                SubscriberReferenceCode = ""  // TODO: Add your Subscriber Reference Code here!
            };

            DFResponseList dfResponseList = dayforceServiceClient.Query(sessionTicket, dfRequest);

            if (dfResponseList.Error.Code == 0)
            {
                foreach (DFNotificationEvent notificationEvent in dfResponseList.Result)
                {
                    OutputThePropertyValues(notificationEvent, "");
                    DFAckNotificationRequest dfAckNotificationRequest = new DFAckNotificationRequest()
                    {
                        NotificationDataId = notificationEvent.NotificationDataId
                    };
                    var dfAckNotificationResponse = dayforceServiceClient.Execute(sessionTicket, dfAckNotificationRequest);
                    if (dfAckNotificationResponse.Error.Code == 0)
                    {
                        Console.WriteLine("{0} ACK");
                    }
                    else
                        Console.WriteLine(dfAckNotificationResponse.Error.Code + " - " + dfAckNotificationResponse.Error.Message);
                }
            }
            else
            {
                Console.WriteLine(dfResponseList.Error.Code + " - " + dfResponseList.Error.Message);
            }

            // Logout
            dayforceServiceClient.Logout(sessionTicket);
        }

        private static void SampleCallToGetEmployeeXRefCodes()
        {
            DayforceServiceClient dayforceServiceClient;
            string sessionTicket;
            int maxQueryResults;

            InitiateSession(clientName, userName, password, out dayforceServiceClient, out sessionTicket, out maxQueryResults);

            if (dayforceServiceClient == null || sessionTicket == null)
                return;

            // Sample showing a request for employees after a particular date
            DFRequest dfRequest = new GetEmployeeXRefCodesRequest()
            {
                FilterHireStartDate = new DateTime(2010, 1, 1)
            };

            DFResponseList dfResponseList = dayforceServiceClient.Query(sessionTicket, dfRequest);

            if (dfResponseList.Error.Code == 0)
            {
                int processedRows = 0;
                while (processedRows < dfResponseList.Result.Length)
                {
                    dfRequest = new GetEmployeesByXRefCodeRequest()
                    {
                        XRefCodes = dfResponseList.Result.Skip(processedRows)
                                                         .Take(maxQueryResults)
                                                         .Select(d => (d as GetEmployeeXRefCodesResponse).XRefCode)
                                                         .ToArray()
                    };

                    DFResponseList dfEmployeeResponseList = dayforceServiceClient.Query(sessionTicket, dfRequest);

                    if (dfEmployeeResponseList.Error.Code == 0)
                    {
                        foreach (Employee employee in dfEmployeeResponseList.Result)
                        {
                            OutputThePropertyValues(employee, "");
                            //Console.WriteLine(employee.FirstName + " " + employee.LastName + " (" + employee.EmployeeNumber + ")");
                        }

                        processedRows = processedRows + maxQueryResults;
                    }
                    else
                    {
                        Console.WriteLine(dfResponseList.Error.Code + " - " + dfResponseList.Error.Message);
                    }
                }
            }
            else
            {
                Console.WriteLine(dfResponseList.Error.Code + " - " + dfResponseList.Error.Message);
            }

            // Logout
            dayforceServiceClient.Logout(sessionTicket);
        }

        private static void SampleCallForReports()
        {
            DayforceServiceClient dayforceServiceClient;
            string sessionTicket;

            InitiateSession(clientName, userName, password, out dayforceServiceClient, out sessionTicket);

            if (dayforceServiceClient == null || sessionTicket == null)
                return;

            // Get the Report Definitions
            DFRequest dfRequest = new GetReportDefinitionsRequest();


            DFResponseList dfResponseList = dayforceServiceClient.Query(sessionTicket, dfRequest);

            ReportDefinition reportToGetDataFor = null;

            if (dfResponseList.Error.Code == 0)
            {

                foreach (ReportDefinition reportDefinition in dfResponseList.Result)
                {
                    DisplayReportDefinition(reportDefinition);

                    // We'll capture the report name of either the first report in the list, or the
                    // last report that has parameters. Using a report with parameters will
                    // exercise more logic within this method.
                    if ((reportDefinition.Parameters != null && reportDefinition.Parameters.Length > 0) || reportToGetDataFor == null)
                        reportToGetDataFor = reportDefinition;
                }
            }
            else
            {
                Console.WriteLine(dfResponseList.Error.Code + " - " + dfResponseList.Error.Message);
                return;
            }

            Console.WriteLine("Prepping to execute the report: " + reportToGetDataFor.ReportName);

            // Build the request object to query for the Report Data
            dfRequest = new GetReportRequest()
            {
                XRefCode = reportToGetDataFor.XRefCode,
                Parameters = GatherParameterValues(reportToGetDataFor).ToArray(),
            };

            Console.WriteLine();

            DFResponse dfResponse = dayforceServiceClient.Execute(sessionTicket, dfRequest);

            if (dfResponse.Error.Code == 0)
            {
                Report report = dfResponse.Result as Report;

                Console.WriteLine("Results of the report: " + reportToGetDataFor.ReportName);
                Console.WriteLine("Report XRefCode: " + report.XRefCode);

                // Output the column names and data types as a comma delimited list
                Console.WriteLine(string.Join(", ", report.ColumnDefinitions.Select(c => c.DisplayName + "(" + c.DataType + ")").ToArray()));

                // Output the row data as a comma delimited list
                foreach (ReportRow row in report.Rows)
                {
                    Console.WriteLine(string.Join(", ", row.ColumnValues));
                }
            }
            else
            {
                Console.WriteLine(dfResponse.Error.Code + " - " + dfResponse.Error.Message);
            }

            // Logout
            dayforceServiceClient.Logout(sessionTicket);

        }

        private static void SampleCallForReportPages()
        {
            DayforceServiceClient dayforceServiceClient;
            string sessionTicket;

            InitiateSession(clientName, userName, password, out dayforceServiceClient, out sessionTicket);

            if (dayforceServiceClient == null || sessionTicket == null)
                return;

            // Get the Report Definitions
            DFRequest dfRequest = new GetReportDefinitionsRequest();

            DFResponseList dfResponseList = dayforceServiceClient.Query(sessionTicket, dfRequest);

            ReportDefinition reportToGetDataFor = null;

            if (dfResponseList.Error.Code == 0)
            {

                foreach (ReportDefinition reportDefinition in dfResponseList.Result)
                {
                    DisplayReportDefinition(reportDefinition);

                    // We'll capture the report name of either the first report in the list, or the
                    // last report that has parameters. Using a report with parameters will
                    // exercise more logic within this method.
                    if ((reportDefinition.Parameters != null && reportDefinition.Parameters.Length > 0) || reportToGetDataFor == null)
                        reportToGetDataFor = reportDefinition;
                }
            }
            else
            {
                Console.WriteLine(dfResponseList.Error.Code + " - " + dfResponseList.Error.Message);
            }


            Console.WriteLine("Prepping to execute the report: " + reportToGetDataFor.ReportName);

            // Open the Report page and set the page size
            dfRequest = new OpenReportPagesRequest()
            {
                XRefCode = reportToGetDataFor.XRefCode,
                Parameters = GatherParameterValues(reportToGetDataFor).ToArray(),
                PageSize = 100
            };

            Console.WriteLine();

            DFResponse dfResponse = dayforceServiceClient.Execute(sessionTicket, dfRequest);

            if (dfResponse.Error.Code == 0)
            {
                OpenReportPagesResponse openResponse = dfResponse.Result as OpenReportPagesResponse;
                Console.WriteLine("The " + reportToGetDataFor.XRefCode + " report has been opened and has the following properties:");
                Console.WriteLine("ReportId: " + openResponse.ReportId);
                Console.WriteLine("PageCount: " + openResponse.PageCount);
                Console.WriteLine("RowCount: " + openResponse.RowCount);

                // Get the Report pages by iterating from 1 to PageCount
                for (int pageNumber = 1; pageNumber <= openResponse.PageCount; pageNumber++)
                {
                    dfRequest = new GetReportPageRequest()
                    {
                        ReportId = openResponse.ReportId,
                        PageNumber = pageNumber
                    };

                    dfResponse = dayforceServiceClient.Execute(sessionTicket, dfRequest);

                    if (dfResponse.Error.Code == 0)
                    {
                        Report report = dfResponse.Result as Report;

                        Console.WriteLine("Report XRefCode: " + report.XRefCode);

                        // Output the column names and data types as a comma delimited list 
                        Console.WriteLine(string.Join(", ", report.ColumnDefinitions.Select(c => c.DisplayName + "(" + c.DataType + ")").ToArray()) +
                            " - Page Number: " + pageNumber);

                        // Output the row data as a comma delimited list
                        foreach (ReportRow row in report.Rows)
                        {
                            Console.WriteLine(string.Join(", ", row.ColumnValues));
                        }
                    }
                    else
                    {
                        Console.WriteLine(dfResponse.Error.Code + " - " + dfResponse.Error.Message);
                    }
                }

                // Close the report to clean it up, reduce security risks
                dfRequest = new CloseReportPagesRequest()
                {
                    ReportId = openResponse.ReportId,
                };
                dfResponse = dayforceServiceClient.Execute(sessionTicket, dfRequest);

                // Logout - this would also cleanup any report data associated with the session
                dayforceServiceClient.Logout(sessionTicket);
            }
            else
            {
                Console.WriteLine(dfResponse.Error.Code + " - " + dfResponse.Error.Message);
            }
        }

        // This method displays the report definition in the console window
        private static void DisplayReportDefinition(ReportDefinition reportDefinition)
        {
            Console.WriteLine("Name: " + reportDefinition.ReportName);
            Console.WriteLine("Description: " + reportDefinition.ReportDescription);
            Console.WriteLine("Report XRefCode: " + reportDefinition.XRefCode);
            Console.WriteLine("Max Rows: " + reportDefinition.MaxRows);
            for (int i = 0; i < reportDefinition.Parameters.Count(); i++)
            {
                ReportParameterDefinition parameterDefinition = reportDefinition.Parameters[i];
                //Console.WriteLine("Parameters: " + string.Join(", ", reportDefinition.Parameters.Select(p => p.Operator + " " + p.Name + " (" + p.DataType + " " + p.UniqueId + ")")));
                Console.WriteLine("Parameters " + (i + 1) + " of " + reportDefinition.Parameters.Count());
                Console.WriteLine("    DataType: " + parameterDefinition.DataType);
                Console.WriteLine("    DefaultValue: " + parameterDefinition.DefaultValue);
                Console.WriteLine("    DisplayName: " + parameterDefinition.DisplayName);
                Console.WriteLine("    IsRequired: " + parameterDefinition.IsRequired);
                Console.WriteLine("    Name: " + parameterDefinition.Name);
                Console.WriteLine("    Operator: " + parameterDefinition.Operator);
                Console.WriteLine("    UniqueId: " + parameterDefinition.UniqueId);

                if (parameterDefinition.AvailableValues != null)
                {
                    for (int k = 0; k < parameterDefinition.AvailableValues.Count(); k++)
                    {
                        Console.WriteLine("    AvailableValues (" + (k + 1) + " of " + parameterDefinition.AvailableValues.Count() + "):");
                        Console.WriteLine("        Id: " + parameterDefinition.AvailableValues[k].Id);
                        Console.WriteLine("        Name: " + parameterDefinition.AvailableValues[k].Name);
                    }
                }
            }
            Console.WriteLine();
        }

        // This method prompts the user for input values for the report parameters. 
        private static List<ReportParameter> GatherParameterValues(ReportDefinition reportToGetDataFor)
        {

            // If the report requires parameters to be set, then prompt the user for those values.
            // For simplicity, this sample is only prompting for required parameters.
            List<ReportParameter> reportParameters = new List<ReportParameter>();
            foreach (ReportParameterDefinition parameterDefinition in reportToGetDataFor.Parameters)
            {
                if (parameterDefinition.IsRequired)
                {
                    Console.Write("[Required] ");
                }
                else
                {
                    Console.Write("[Optional] ");
                }

                if (parameterDefinition.AvailableValues != null)
                {
                    List<ListValue> availableValues = parameterDefinition.AvailableValues.ToList();
                    Console.WriteLine("Choose a value for " + parameterDefinition.Operator + " "
                            + parameterDefinition.Name + ": ");

                    for (int k = 0; k < availableValues.Count; k++)
                    {
                        Console.WriteLine("  " + availableValues[k].Id + " = " + availableValues[k].Name);
                    }
                }
                else
                {
                    Console.WriteLine("Enter a value for "
                            + (parameterDefinition.Operator == null ? "" : parameterDefinition.Operator) + " "
                            + parameterDefinition.Name + " (" + parameterDefinition.DataType + "): ");
                }

                Console.WriteLine("  Press Enter to use the DefaultValue: " + parameterDefinition.DefaultValue);

                string userValue = Console.ReadLine();
                if (userValue == "null")
                    reportParameters.Add(new ReportParameter() { UniqueId = parameterDefinition.UniqueId, Value = null });
                else if (userValue == "") // user just hit enter, use the default value if there is one.
                    reportParameters.Add(new ReportParameter() { UniqueId = parameterDefinition.UniqueId, Value = parameterDefinition.DefaultValue });
                else
                    reportParameters.Add(new ReportParameter() { UniqueId = parameterDefinition.UniqueId, Value = userValue });
            }

            return reportParameters;
        }

        private static void SampleCallToGetNotificationData()
        {
            DayforceServiceClient dayforceServiceClient;
            string sessionTicket;

            InitiateSession(clientName, userName, password, out dayforceServiceClient, out sessionTicket);

            if (dayforceServiceClient == null || sessionTicket == null)
                return;

            // Sample showing a request for employees after a particular date
            DFNotificationDataRequest dfRequest = new DFNotificationDataRequest();
            dfRequest.NotificationDataIds = new int[3] { 1, 2, 3 };

            DFResponseList dfResponseList = dayforceServiceClient.Query(sessionTicket, dfRequest);

            if (dfResponseList.Error.Code == 0)
            {
                foreach (DFNotificationEvent notificationEvent in dfResponseList.Result)
                {
                    OutputThePropertyValues(notificationEvent, "");
                    DFAckNotificationRequest dfAckNotificationRequest = new DFAckNotificationRequest()
                    {
                        NotificationDataId = notificationEvent.NotificationDataId
                    };
                    var dfAckNotificationResponse = dayforceServiceClient.Execute(sessionTicket, dfAckNotificationRequest);
                    if (dfAckNotificationResponse.Error.Code == 0)
                    {
                        Console.WriteLine("{0} ACK");
                    }
                    else
                        Console.WriteLine(dfAckNotificationResponse.Error.Code + " - " + dfAckNotificationResponse.Error.Message);
                }
            }
            else
            {
                Console.WriteLine(dfResponseList.Error.Code + " - " + dfResponseList.Error.Message);
            }

            // Logout
            dayforceServiceClient.Logout(sessionTicket);
        }


        private static void InitiateSession(string clientName, string userName, string password, out DayforceServiceClient dayforceServiceClient, out string sessionTicket)
        {
            int maxQueryResults;
            InitiateSession(clientName, userName, password, out dayforceServiceClient, out sessionTicket, out maxQueryResults);
        }
        private static void InitiateSession(string clientName, string userName, string password, out DayforceServiceClient dayforceServiceClient, out string sessionTicket, out int maxQueryResults)
        {
            maxQueryResults = -1;
            sessionTicket = null;
            dayforceServiceClient = new DayforceServiceClient("DayforceService");

            // Make the initial call to determine which Uri to use to access the appropriate site
            DFGetClientSiteUriResponse getClientSiteUriResponse = dayforceServiceClient.GetClientSiteUri(clientName);

            if (getClientSiteUriResponse.Error.Code != 0)
            {
                Console.WriteLine("Error occurred getting client site Uri: " +
                                    getClientSiteUriResponse.Error.Message + "(" + getClientSiteUriResponse.Error.Code + ")");
                dayforceServiceClient = null;
                return;
            }

            // If the Uri returned from the GetClientSiteUri does not match the Uri used to initially connect to the 
            // Dayforce Services, then re-instantiate the client proxy using the new Uri.
            if (!getClientSiteUriResponse.Uri.Equals(dayforceServiceClient.Endpoint.Address.Uri.AbsoluteUri,
                                                        StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("Connecting to DayforceService located at Uri " + getClientSiteUriResponse.Uri);
                dayforceServiceClient = new DayforceServiceClient("DayforceService", getClientSiteUriResponse.Uri);
            }

            // Authenticate and start a session instance
            DFAuthenticateResponse authenticateResponse = dayforceServiceClient.Authenticate(clientName, userName, password);

            if (authenticateResponse.Error.Code != 0)
            {
                Console.WriteLine("Authentication failure: " + authenticateResponse.Error.Message + "(" + authenticateResponse.Error.Code + ")");
                return;
            }

            sessionTicket = authenticateResponse.SessionTicket;
            maxQueryResults = authenticateResponse.MaximumQueryResults;
        }

        private static void OutputThePropertyValues(DFObject response, string prefix)
        {
            if (response == null)
                return;  // if the subordinate object is null, write out nothing.

            Type theType = response.GetType();

            PropertyInfo[] propertyInfos = theType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            Console.WriteLine(prefix + theType.Name);

            prefix += "   ";

            // use reflection to inspect each field
            foreach (PropertyInfo propInfo in propertyInfos)
            {
                if (propInfo.PropertyType.IsArray)
                {
                    // iterate over the collection of child objects (ex. EmployeeAddresses)
                    DFObject[] array = (DFObject[])propInfo.GetValue(response);
                    for (int i = 0; array != null && i < array.Length; i++)
                    {
                        OutputThePropertyValues(array[i], prefix);
                    }
                }
                else if (propInfo.PropertyType.IsSubclassOf(typeof(DFObject)))
                {
                    // process the single child object (ex. EmployeeBadge)
                    OutputThePropertyValues((DFObject)propInfo.GetValue(response), prefix);
                }
                else if (propInfo.PropertyType.Name != "ExtensionDataObject")
                {
                    // Out the value of property
                    Console.WriteLine(prefix + propInfo.Name + ": " + propInfo.GetValue(response));
                }
            }
        }
    }
}
