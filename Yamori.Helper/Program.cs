using Yamori.Core.Sensors.Service;
using Yamori.Helper;

if (ConsentCommand.Matches(args))
{
    // GUI が管理者の承認（UAC）を得て起動したときは、同意を変えるだけで終わる。承認されずに動いたときは断る。
    return HelperPrivilege.IsSufficient() ? await ConsentCommand.RunAsync(args) : (int)SensorSettingResult.AccessDenied;
}

if (!HelperPrivilege.IsSufficient())
{
    // データフォルダーの ACL（SYSTEM と Administrators だけ）と PawnIO には、管理者以上の権限が要る。
    Console.Error.WriteLine("Yamori Helper は、サービス（LocalSystem）か、管理者として実行したコンソールから起動してください。");
    return 1;
}

var builder = Host.CreateApplicationBuilder(args);

// サービスとしても、管理者のコンソールからの起動（開発中のデバッグ）としても動く。
builder.Services.AddWindowsService(options => options.ServiceName = HelperServiceInfo.ServiceName);
builder.Services.AddYamoriHelper();

var host = builder.Build();
await host.RunAsync();
return 0;
