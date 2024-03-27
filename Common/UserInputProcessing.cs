using vut_ipk1.Common.Enums;
using vut_ipk1.Common.Interfaces;

namespace vut_ipk1.Common;

public class UserInputProcessing(
    IConnection connection
)
{
    public async Task ProcessUserInputAsync()
    {
        while (true)
        {
            var input = await Console.In.ReadLineAsync();
        
            if (input == null)
                continue;
        
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

                        var splitArguments = arguments.Split(' ');
                        if (splitArguments.Length != 3)
                        {
                            await Console.Error.WriteLineAsync(ErrorMessage.InvalidCommandUsage);
                            continue;
                        }
                        
                        if (!ValidateStringAlphanumWithDash(splitArguments[0]) || splitArguments[0].Length > 20)
                        {
                            await Console.Error.WriteLineAsync("Invalid username.");
                            continue;
                        }
                        if (!ValidateStringAlphanumWithDash(splitArguments[1]) || splitArguments[1].Length > 128)
                        {
                            await Console.Error.WriteLineAsync("Invalid secret.");
                            continue;
                        }
                        if (!ValidateStringWithAsciiRange(splitArguments[2], 0x21, 0x7E) || splitArguments[2].Length > 20)
                        {
                            await Console.Error.WriteLineAsync("Invalid display name.");
                            continue;
                        }
        
                        await connection.Auth(splitArguments[0], splitArguments[2], splitArguments[1]);
        
                        break;
                    case "/join":
                        if (arguments == null)
                        {
                            await Console.Error.WriteLineAsync(ErrorMessage.InvalidCommandUsage);
                            continue;
                        }
                        
                        if (!ValidateStringAlphanumWithDash(arguments) || arguments.Length > 20)
                        {
                            await Console.Error.WriteLineAsync("Invalid channel name.");
                            continue;
                        }
                        
                        await connection.Join(arguments);
                        
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
                        
                        connection.Rename(arguments);
                    
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
                    await Console.Error.WriteLineAsync("Invalid message format.");
                    continue;
                }
                
                await connection.SendMessage(input);
            }
        }
    }
        
    private static bool ValidateStringWithAsciiRange(string input, int min, int max)
    {
        return input.ToCharArray().All(character => character >= min && character <= max);
    }
    
    private static bool ValidateStringAlphanumWithDash(string input)
    {
        return input.ToCharArray().All(character => character == 0x2D || char.IsLetterOrDigit(character));
    }
}