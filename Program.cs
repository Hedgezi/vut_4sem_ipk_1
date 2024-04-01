using System.Net;
using System.Net.Sockets;
using CommandLine;
using vut_ipk1.Common;
using vut_ipk1.Common.Interfaces;
using vut_ipk1.Tcp;
using vut_ipk1.Udp;

namespace vut_ipk1;

class Program
{
    private static IConnection? _connection;

    private static async Task<int> Main(string[] args)
    {
        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true;
            
            if (_connection != null)
                await _connection.EndSession();

            await Console.Error.WriteLineAsync("Exiting...");
            Environment.Exit(0);
        };

        var options = new CommandLineOptions();
        Parser.Default.ParseArguments<CommandLineOptions>(args)
            .WithParsed(o => options = o);

        var hostname = GetProperIpAddress(options.ServerHostname);

        _connection = options.ProtocolType switch
        {
            ProtocolType.udp => new UdpConnection(
                hostname,
                options.ServerPort,
                options.Timeout,
                options.Retransmissions
            ),
            ProtocolType.tcp => new TcpConnection(
                hostname,
                options.ServerPort
            ),
        };
        var userInputProcessing = new UserInputProcessing(_connection);

        var connectionMainLoopTask = _connection.MainLoopAsync();
        var userInputProcessingTask = userInputProcessing.ProcessUserInputAsync();

        var result = await await Task.WhenAny(connectionMainLoopTask, userInputProcessingTask);
        return result;
    }
    
    private static IPAddress GetProperIpAddress(string hostname)
    {
        var ipAddress = Dns.GetHostEntry(hostname).AddressList[0];

        foreach (var address in Dns.GetHostEntry(hostname).AddressList)
        {
            if (address.AddressFamily != AddressFamily.InterNetwork)
                continue;
            
            ipAddress = address;
            break;
        }

        return ipAddress;
    }
}