using Yamori.Helper;

var builder = Host.CreateApplicationBuilder(args);

// サービスとしても、管理者のコンソールからの起動（開発中のデバッグ）としても動く。
builder.Services.AddWindowsService(options => options.ServiceName = HelperServiceInfo.ServiceName);

var host = builder.Build();
host.Run();
