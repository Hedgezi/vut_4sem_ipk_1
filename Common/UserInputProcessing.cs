using vut_ipk1.Common.Enums;
using vut_ipk1.Common.Interfaces;

namespace vut_ipk1.Common;

public class UserInputProcessing(
    IConnection connection
)
{
    private IConnection _connection = connection;
    
    public async Task<int> ProcessUserInputAsync()
    {
        while (true)
        {
            var input = await Console.In.ReadLineAsync();

            if (input == null) // if EOF, end connection
            {
                await _connection.EndSession();
                
                return 0;
            }
        
            if (input.StartsWith('/'))
            {
                var splitInput = input.Split(' ', 2);
                var command = splitInput[0];
                var arguments = splitInput.Length > 1 ? splitInput[1].TrimEnd() : null;
        
                switch (command)
                {
                    case "/auth":
                        if (arguments == null)
                        {
                            await Console.Error.WriteLineAsync(ErrorMessage.InvalidCommandUsage);
                            continue;
                        }
                        
                        var splitArguments = arguments.Split(' ', 3);

                        if (splitArguments.Length != 3)
                        {
                            await Console.Error.WriteLineAsync(ErrorMessage.InvalidCommandUsage);
                            continue;
                        }

                        if (!ValidateStringAlphanumWithDash(splitArguments[0]) || splitArguments[0].Length > 20)
                        {
                            await Console.Error.WriteLineAsync(ErrorMessage.InvalidUsername);
                            continue;
                        }
                        if (!ValidateStringAlphanumWithDash(splitArguments[1]) || splitArguments[1].Length > 128)
                        {
                            await Console.Error.WriteLineAsync(ErrorMessage.InvalidSecret);
                            continue;
                        }
                        if (!ValidateStringWithAsciiRange(splitArguments[2], 0x21, 0x7E) || splitArguments[2].Length > 20)
                        {
                            await Console.Error.WriteLineAsync(ErrorMessage.InvalidDisplayName);
                            continue;
                        }
        
                        await _connection.Auth(splitArguments[0], splitArguments[2], splitArguments[1]);
        
                        break;
                    case "/join":
                        if (arguments == null)
                        {
                            await Console.Error.WriteLineAsync(ErrorMessage.InvalidCommandUsage);
                            continue;
                        }
                        
                        if (!ValidateStringAlphanumWithDash(arguments) || arguments.Length > 20)
                        {
                            await Console.Error.WriteLineAsync(ErrorMessage.InvalidChannelName);
                            continue;
                        }
                        
                        await _connection.Join(arguments);
                        
                        break;
                    case "/rename":
                        if (arguments == null)
                        {
                            await Console.Error.WriteLineAsync(ErrorMessage.InvalidCommandUsage);
                            continue;
                        }

                        if (!ValidateStringWithAsciiRange(arguments, 0x21, 0x7E) || arguments.Length > 20)
                        {
                            await Console.Error.WriteLineAsync("Invalid display name.");
                            continue;
                        }
                        
                        _connection.Rename(arguments);
                    
                        break;
                    case "/help":
                        if (arguments != null)
                        {
                            await Console.Error.WriteLineAsync(ErrorMessage.InvalidCommandUsage);
                            continue;
                        }
                        
                        await Console.Error.WriteLineAsync("Available commands:");
                        await Console.Error.WriteLineAsync("/auth <username> <secret> <display name>");
                        await Console.Error.WriteLineAsync("/join <channel name>");
                        await Console.Error.WriteLineAsync("/rename <new display name>");
                        await Console.Error.WriteLineAsync("/help");
                        await Console.Error.WriteLineAsync("/exit");
                        break;
                    default:
                        await Console.Error.WriteLineAsync(ErrorMessage.InvalidCommandUsage);
                        break;
                }
            }
            else
            {
                if (!ValidateStringWithAsciiRange(input, 0x20, 0x7E) || input.Length > 1400)
                {
                    await Console.Error.WriteLineAsync(ErrorMessage.InvalidMessage);
                    continue;
                }
                
                await _connection.SendMessage(input);
            }
        }
    }
        
    private static bool ValidateStringWithAsciiRange(string input, int min, int max)
    {
        return input.ToCharArray().All(character => character >= min && character <= max);
    }
    
    private static bool ValidateStringAlphanumWithDash(string input)
    {
        return input.ToCharArray().All(character => character == 0x2D || (character >= 0x30 && character <= 0x39) || (character >= 0x41 && character <= 0x5A) || (character >= 0x61 && character <= 0x7A));
    }
}