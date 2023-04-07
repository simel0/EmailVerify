using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using CheckEmailExist.Helpers;
using DnsClient;
using DnsClient.Protocol;

namespace CheckEmailExist.Services;

public class EmailValidator
{
    private const string EmailFrom = "longnt2204@gmail.com";

    private static readonly LookupClient _lookupClient = new(
        new LookupClientOptions(NameServer.GooglePublicDns2, NameServer.GooglePublicDns, NameServer.Cloudflare2,
            NameServer.Cloudflare)
        {
            UseCache = true,
            UseRandomNameServer = false,
            Retries = 3
        });

    private readonly int DefaultTcpPort = 25;

    public bool Validate(string input)
    {
        if (!MailAddress.TryCreate(input, out var email))
        {
            return false;
        }

        if (email.Host.EndsWith(".") || !email.Host.Contains("."))
        {
            return false;
        }

        var mxRecords = AsyncHelpers.RunSync(() => _lookupClient.QueryAsync(email.Host, QueryType.MX)).Answers
            .MxRecords()?.ToList();

        if (mxRecords == null || mxRecords.Count == 0)
        {
            return false;
        }

        foreach (var mxRecord in mxRecords)
        {
            try
            {
                if (CheckEmailExist(email, mxRecord))
                {
                    return true;
                }
            }
            catch (Exception e)
            {
            }
        }

        return false;
    }

    private bool CheckEmailExist(MailAddress email, MxRecord mxRecord)
    {
        using (var tcpClient = new TcpClient(mxRecord.Exchange, DefaultTcpPort))
        {
            using (var networkStream = tcpClient.GetStream())
            using (var streamReader = new StreamReader(networkStream))
            {
                var startResponse = AcceptResponse(streamReader, SmtpStatusCode.ServiceReady);

                var response1 = SendCommand(networkStream, streamReader, "HELO " + email.Host, SmtpStatusCode.Ok);
                var response2 = SendCommand(networkStream, streamReader, "MAIL FROM:<" + EmailFrom + ">",
                    SmtpStatusCode.Ok);
                var response3 = SendCommand(networkStream, streamReader, "RCPT TO:<" + email.Address + ">");
                var response4 = SendCommand(networkStream, streamReader, "RSET", SmtpStatusCode.Ok);

                return response3.Code != SmtpStatusCode.MailboxUnavailable &&
                       response3.Code != SmtpStatusCode.MailboxNameNotAllowed;
            }
        }
    }

    private SmtpResponse SendCommand(NetworkStream networkStream, StreamReader streamReader, string command,
        params SmtpStatusCode[] goodReplys)
    {
        var dataBuffer = Encoding.ASCII.GetBytes(command + "\r\n");
        networkStream.Write(dataBuffer, 0, dataBuffer.Length);

        return AcceptResponse(streamReader, goodReplys);
    }

    private SmtpResponse AcceptResponse(StreamReader streamReader, params SmtpStatusCode[] goodReplys)
    {
        var response = streamReader.ReadLine();

        if (string.IsNullOrEmpty(response) || response.Length < 3)
        {
            throw new Exception("Invalid response");
        }

        var smtpStatusCode = GetResponseCode(response);

        if (goodReplys.Length > 0 && !goodReplys.Contains(smtpStatusCode))
        {
            throw new Exception(response);
        }

        return new SmtpResponse
        {
            Raw = response,
            Code = smtpStatusCode
        };
    }

    private SmtpStatusCode GetResponseCode(string response)
    {
        return (SmtpStatusCode)Enum.Parse(typeof(SmtpStatusCode), response.Substring(0, 3));
    }

    private struct SmtpResponse
    {
        public string Raw { get; set; }
        public SmtpStatusCode Code { get; set; }
    }
}
