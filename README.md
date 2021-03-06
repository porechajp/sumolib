# SumoLib

This library provides higher level interface to [Sumologic](https://www.sumologic.com/) Search API. Sumologic is a wonderful system to ingest and process textual logs for systems monitoring, debugging, telemetery and log insights.

Sumologic provides REST based [Search Apis](https://help.sumologic.com/APIs/Search-Job-API/About-the-Search-Job-API) to fire the queries on your log data.

The APIs are bit tricky and requires stateful communication of three request-response pairs. The method is outlined [here](https://help.sumologic.com/APIs/Search-Job-API/About-the-Search-Job-API#process-flow).

SumoLib hides all this complexity and provides the `IEnumerable<T>` to process the results using LINQ / loops.

Sumolib is available on Nuget at : https://www.nuget.org/packages/SumoLib/

## End Point Details

Sumologic provides REST API [endpoints](https://help.sumologic.com/APIs/General-API-Information/Sumo-Logic-Endpoints-and-Firewall-Security) which would be specific to your account, so please contact your account admin for getting the correct API Url. 

The API endpoing is protected using Access ID and Access Key, which could be generated by following [instructions](https://help.sumologic.com/Manage/Security/Access-Keys) given in Sumologic help

Once you have all three : API URL, Access ID, and Access Key. SumoLib can be configured to use them.,

```csharp
using SumoLib;
using SumoLib.Config;

var config = new EndPointConfig("accessId","accessKey","apiUrl");

SumoClient.SetupDefault(config);
```

In case if you don't want to use the same config for entier runtime of your app, you could also pass the config optionally as shown in below examples.

## Querying

If you are impatient, you could directly jump to [Examples](#examples) !

Querying SumoLogic involves three important things,

1. Query
2. Time Range
3. Stream of Result Data

The Query must be in the syntax as prescribed Sumologic Query Language. 

### Time Range

Time Range can be specified in relative or absolute terms. The relative time range can be specified as "In Last 5 days", "In Last 5 Months", etc... 

In case of absolute terms, you may provide From and To `DateTime` values.

Please note that Sumologic works best if the times are provided in UTC so in case if you provide `DateTime` with `DateTimeKind.Local` then the library will internally convert the value to UTC using `DateTime.ToUniversalTime()`.

In case if `DateTime.Kind` is `DateTimeKind.Unspecified` then library will just interpret the `DateTime` value provided as UTC.

For example,

if your machine's local timezone is IST (UTC+5:30) then following pair of dates,

```csharp
DateTime from = new DateTime(2020,12,30,0,0,0,DateTimeKind.Local);
DateTime to = new DateTime(2020,12,31,0,0,0,DateTimeKind.Local);
```

would be converted to UTC with effective values of *from* being 29-Dec-2020 18:30:00 and *to* being 30-Dec-2020 18:30:00

However if you have not specified the kind,

```csharp
DateTime from = new DateTime(2020,12,30,0,0,0);
DateTime to = new DateTime(2020,12,31,0,0,0);
```
The same values will be interpreted as it is. So *from* would be 30-Dec-2020 0:0:0 and *to* would be 31-Dec-2020 0:0:0 


## Examples

Given the default end point config has already been setup, you could execute a simple query following way,

```csharp
using SumoLib;

var data = await SumoClient.New().Query(@"_sourceCategory=appserver | parse""Login:*"" as uid | fields uid")
                                 .ForLast(TimeSpan.FromDays(1))
                                 .RunAsync(new {uid = ""});

Console.WriteLine($"Total Logins : {data.Stats.MessageCount}");
foreach (var record in data)
{
    Console.WriteLine(record.uid);
}

```
In case if you have defined entity,

```csharp
public class LoginData
{
    public string Uid { get; set; }
    public string Source { get; set; }
}

```
then you could use the generic RunAsync method,

```csharp

var data = await SumoClient.New().Query(@"_sourceCategory=*/appserver | 
                                        parse""Login:*"" as uid | 
                                        _sourceCategory as source 
                                        | fields uid,source")
                                 .ForLast(TimeSpan.FromDays(1))
                                 .RunAsync<LoginData>();

Console.WriteLine($"Total Logins : {data.Stats.MessageCount}");

foreach (var record in data)
{
    Console.WriteLine($"{record.Uid} logged in {record.Source}");
}
 

```
It is important to note that library will map fields **case insensitively**. This will nicely bridge the difference between the naming conventions of Sumologic query fields (which are typically camelCase) and C# class properties (typically PascalCase).

So the `uid` field in above example from Sumologic query would be mapped `LoginData.Uid` automatically.

### Query Builder

For the beginners to Sumologic, there is an easier alternative to write Sumologic query in this library using fluent pattern. 

For example,

```csharp
using SumoLib;


var query =  SumoClient.New().Builder
                .FromSource("(_sourceCategory=env*/webapp OR _sourceCategory=env*/batch)")
                .Filter("Authentication")
                .Parse("[*:*]", "uid", "fname")
                .And("fields uid,fname")
                .Build()
   
// The builder will generate following query
// (_sourceCategory=env*/webapp OR _sourceCategory=env*/batch) "Authentication" | parse "[*:*]" as uid,fname | fields uid,fname

Console.WriteLine(query.Text);

var data = await query.ForLast(TimeSpan.FromDays(7))
                      .RunAsync(new {uid = "", fname = ""});


```

The builder also has Where function to generate the where clauses easily,

```csharp
using SumoLib;


var query =  SumoClient.New().Builder
                .FromSource("(_sourceCategory=env*/webapp OR _sourceCategory=env*/batch)")
                .Filter("Authentication")
                .Parse("[*:*]", "uid", "fname")
                .Where("fname").Matches("Robert")
                .And("fields uid,fname")
                .Build()
   
// The builder will generate following query (searching only user logins whose name Robert)
// (_sourceCategory=env*/webapp OR _sourceCategory=env*/batch) "Authentication" | parse "[*:*]" as uid,fname | where fname matches "Robert" | fields uid,fname

Console.WriteLine(query.Text);

var data = await query.ForLast(TimeSpan.FromDays(7))
                      .RunAsync(new {uid = "", fname = ""});


```

## Conclusion

Sumolib provides a very idiomatic access to Sumologic search APIs and would help the developers to create innovative applications for monitoring, analytics, troubleshooting, etc... backed by Sumologic's powerful search backend.

Sumolib is available on Nuget at : https://www.nuget.org/packages/SumoLib/