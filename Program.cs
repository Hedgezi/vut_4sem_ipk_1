using System.Net;
using CommandLine;
using vut_ipk1.Common;
using vut_ipk1.Common.Interfaces;
using vut_ipk1.UdpClient;

namespace vut_ipk1;

class Program
{
    private static async Task Main(string[] args)
    {
        var options = new CommandLineOptions();
        Parser.Default.ParseArguments<CommandLineOptions>(args)
            .WithParsed(o => options = o)
            .WithNotParsed(errors => { throw new System.Exception("Invalid command line arguments."); });

        IConnection connection = options.ProtocolType switch
        {
            ProtocolType.udp => new UdpConnection(
                IPAddress.Parse(options.ServerHostname),
                options.ServerPort,
                options.Timeout,
                options.Retransmissions
            ),
            _ => throw new System.Exception("Invalid protocol type.")
        };
        var userInputProcessing = new UserInputProcessing(connection);

        var connectionMainLoopTask = connection.MainLoopAsync();
        var userInputProcessingTask = userInputProcessing.ProcessUserInputAsync();

        await Task.WhenAny(connectionMainLoopTask, userInputProcessingTask);
    }
}