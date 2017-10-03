using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using CodeHollow.AzureBillingApi;
using SendGrid;
using SendGrid.Helpers.Mail;

static string CURRENCY_CHAR = "&pound;";
static string FROM = "billingapi@codehollow.com";
static string FROM_NAME = "Azure Billing API";
static string TO = "benc.ms@outlook.com";
//static string TO = "ben.coleman@microsoft.com";
static string TO_NAME = "Ben Coleman";
static string APIKEY = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
static string CLIENT_ID = Environment.GetEnvironmentVariable("BILLING_API_CLIENT");
static string CLIENT_SECRET = Environment.GetEnvironmentVariable("BILLING_API_SECRET");
static string TENANT_ID = Environment.GetEnvironmentVariable("AZURE_TENANT");
static string TENANT_NAME = "microsoft.onmicrosoft.com";
//
// Code taken from https://codehollow.com/2017/09/weekly-azure-billing-report-per-mail-azure-functions/
// And modified so it worked ;)
//
public static void Run(TimerInfo myTimer, TraceWriter log, ExecutionContext context)
{
    log.Info($"### Azure Billing Report function triggered started at: {DateTime.Now}");
    try
    {
        Client c = new Client(TENANT_NAME, CLIENT_ID,
            CLIENT_SECRET, TENANT_ID, "http://localhost/billingapi");

        log.Info($"### Connected to AAD");

        var path = System.IO.Path.Combine(context.FunctionAppDirectory, "azureBillingReport\\report.html");
        string html = GetHtmlReport(c, path, log);

        SendMail(TO, TO_NAME, html, log);
    }
    catch(Exception ex)
    {
        log.Error(ex.Message, ex);
    }
}

//
//
//
public static void SendMail(string toMail, string toName, string html, TraceWriter log)
{
    log.Info($"### Sending HTML report as email via SendGrid...");
    var client = new SendGridClient(APIKEY);
    var from = new EmailAddress(FROM, FROM_NAME);
    var to = new EmailAddress(toMail, toName);

    var msg = MailHelper.CreateSingleEmail(from, to, "Weekly Azure Billing Report", "", html);
    client.SendEmailAsync(msg).Wait();
}

//
//
//
public static string GetHtmlReport(Client c, string htmlFile, TraceWriter log)
{
    log.Info($"### Report generation started...");
    var startdate = DateTime.Now.AddDays(-7); // set start date to last monday.
    var enddate = DateTime.Now.AddDays(-1);   // set end date to last day which is sunday

    var allcosts = c.GetResourceCosts("MS-AZR-0003P", "GBP", "en-US", "GB", startdate, enddate,
    CodeHollow.AzureBillingApi.Usage.AggregationGranularity.Daily, true);

    var costsByResourceGroup = allcosts.GetCostsByResourceGroup();

    string cbr = CostsByResourceGroup(costsByResourceGroup);
    string costDetails = CostsDetails(costsByResourceGroup);

    string html = System.IO.File.ReadAllText(htmlFile);
    html = html.Replace("{costsPerResourceGroup}", cbr);
    html = html.Replace("{costsDetails}", costDetails);
    html = html.Replace("{date}", startdate.ToShortDateString() + " - " + enddate.ToShortDateString());

    return html.ToString();
}

//
//
//
public static string CostsDetails(Dictionary<string, IEnumerable<ResourceCosts>> costsByResourceGroup)
{
    var costs = from cbrg in costsByResourceGroup
                from costsByResourceName in cbrg.Value.GetCostsByResourceName()
                from costsByMeterName in costsByResourceName.Value.GetCostsByMeterName()
                select new
                {
                    ResourceGroup = cbrg.Key,
                    Resource = costsByResourceName.Key,
                    MeterName = costsByMeterName.Key,
                    Costs = costsByMeterName.Value
                };

    var data = costs.Select(x => $"<tr><td>{x.ResourceGroup}</td><td>{x.Resource}</td><td>{x.MeterName}</td><td>{ToHtml(x.Costs.GetTotalUsage())}</td><td>{ToHtml(x.Costs.GetTotalCosts(), true)}</td></tr>");
    return string.Concat(data);
}

//
//
//
public static string CostsByResourceGroup(Dictionary<string, IEnumerable<ResourceCosts>> costsByResourceGroup)
{
    var data = costsByResourceGroup.Select(x => $"<tr><td>{x.Key}</td><td>{ToHtml(x.Value.GetTotalCosts(), true)}</td></tr>");
    return string.Concat(data);
}


public static string ToHtml(this double value, bool currency = false)
{
    if (currency)
        return CURRENCY_CHAR + value.ToString("0.00");

    return value.ToString("0.###");
}
