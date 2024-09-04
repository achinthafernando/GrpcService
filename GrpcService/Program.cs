using Grpc.Core;
using GrpcService;
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

app.Lifetime.ApplicationStarted.Register(() =>
{
    // Resolve logger from dependency injection
    var logger = app.Services.GetRequiredService<ILogger<GreeterService>>();

    // Start the gRPC server
    IPAddress ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork) ?? IPAddress.Loopback;
    
    var grpcServer = new Server
    {
        Services = { Greeter.BindService(new GreeterService(logger)) },
        Ports = { new ServerPort(ipAddress.ToString(), 8090, ServerCredentials.Insecure) }
    };

    grpcServer.Start();
    logger.LogInformation($"Grpc service started on {ipAddress}:8090");

    // Start the UDP server in a separate task
    Task.Run(() =>
    {
        try
        {
            UdpClient udpServer = new UdpClient(new IPEndPoint(ipAddress, 8888));
            Console.WriteLine($"UDP Server 1 listening on {ipAddress}:8888");

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
