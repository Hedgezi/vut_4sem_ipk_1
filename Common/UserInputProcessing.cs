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
        
                        await connection.Auth(splitArguments[0], splitArguments[1], splitArguments[2]);
        
                        break;
                    case "/join":
                        if (arguments == null)
                        {
                            await Console.Error.WriteLineAsync(ErrorMessage.InvalidCommandUsage);
                            continue;
                        }
                        
                        if (arguments.ToCharArray().Any(character => character < 33 || character > 126))
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

                        if (arguments.ToCharArray().Any(character => character < 33 || character > 126))
                        {
                            await Console.Error.WriteLineAsync("Invalid display name.");
                            continue;
                        }
                        
                        connection.Rename(arguments);
                    
                        break;
                    default:
                        await Console.Error.WriteLineAsync("Invalid command.");
                        break;
                }
            }
            else
            {
                if (input.ToCharArray().Any(character => character < 32 || character > 126))
                {
                    await Console.Error.WriteLineAsync("Invalid message format.");
                    continue;
                }
                
                await connection.SendMessage(input);
            }
        }
    }
}