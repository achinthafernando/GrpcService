using GrpcService.Services;
using System.Net;
using System.Net.Sockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

var kestrelConfig = builder.Configuration.GetSection("Kestrel:Grpc");
var ipAddressSrting = kestrelConfig.GetValue<string>("IPAddress") ?? "";
var url = kestrelConfig.GetValue<string>("url") ?? "";
var port = kestrelConfig.GetValue<string>("Port") ?? "";
var gRpcServiceName = kestrelConfig.GetValue<string>("ServiceName") ?? "";

app.Urls.Add($"{url}:{port}");


app.Lifetime.ApplicationStarted.Register(() =>
{
    // Resolve logger from dependency injection
    var logger = app.Services.GetRequiredService<ILogger<GreeterService>>();

    // Start the UDP server in a separate task
    Task.Run(() =>
    {
        try
        {
            var udpConfig = builder.Configuration.GetSection("Kestrel:Udp");
            int.TryParse(udpConfig.GetValue<string>("Port") ?? "", out int udpPort);

            IPAddress ipAddress = IPAddress.Parse(ipAddressSrting);
            UdpClient udpServer = new UdpClient(new IPEndPoint(ipAddress, udpPort));
            Console.WriteLine($"Udp {gRpcServiceName} listening on {ipAddressSrting}:{udpPort.ToString()}");

            while (true)
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] clientRequestData = udpServer.Receive(ref clientEndPoint);
                var message = "Response";
                logger.LogInformation($"Received {message}");
                byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                udpServer.Send(messageBytes, messageBytes.Length, clientEndPoint);
            }
        }
        catch (SocketException ex)
        {
            logger.LogError($"SocketException: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError($"Exception: {ex.Message}");
        }
    });
});
app.Run();
