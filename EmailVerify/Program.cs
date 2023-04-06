// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using DnsClient;

string domainName = "niteco.se";
var lookup = new LookupClient(NameServer.Cloudflare,NameServer.GooglePublicDns2);
var result = await lookup.QueryAsync(domainName, QueryType.MX);

var records = result.Answers.MxRecords().ToList();

Console.WriteLine(JsonSerializer.Serialize(records,new JsonSerializerOptions()
{
    WriteIndented = true
}));
