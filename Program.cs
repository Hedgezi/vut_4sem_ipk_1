﻿using System.Net;
using CommandLine;
using vut_ipk1.Common;
using vut_ipk1.Common.Interfaces;
using vut_ipk1.Tcp;
using vut_ipk1.Udp;

namespace vut_ipk1;

class Program
{
    private static IConnection _connection;
    
    private static async Task Main(string[] args)
    {
        var options = new CommandLineOptions();
        Parser.Default.ParseArguments<CommandLineOptions>(args)
            .WithParsed(o => options = o)
            .WithNotParsed(errors => { throw new System.Exception("Invalid command line arguments."); });
        
        var hostname = Dns.GetHostEntry(options.ServerHostname).AddressList[0];

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
                options.ServerPort,
                options.Timeout,
                options.Retransmissions
            ),
            _ => throw new System.Exception("Invalid protocol type.")
        };
        var userInputProcessing = new UserInputProcessing(_connection);
        
        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true;
            
            await _connection.EndSession();
            
            Console.WriteLine("Exiting...");
            Environment.Exit(0);
        };

        var connectionMainLoopTask = _connection.MainLoopAsync();
        var userInputProcessingTask = userInputProcessing.ProcessUserInputAsync();
        
        await Task.WhenAny(connectionMainLoopTask, userInputProcessingTask);
    }
}