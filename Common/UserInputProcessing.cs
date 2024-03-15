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
            var input = await Console.In.ReadLineAsync(); // TODO: Trim input
        
            if (input == null)
                continue;
        
            if (input.StartsWith('/'))
            {
                var splitInput = input.Split(' ', 2);
                var command = splitInput[0];
                var arguments = splitInput.Length > 1 ? splitInput[1] : null;
        
                switch (command)
                {
                    case "/auth":
                        if (arguments == null)
                        {
                            await Console.Error.WriteLineAsync("Invalid command usage.");
                            continue;
                        }
        
                        var splitArguments = arguments.Split(' ');
                        if (splitArguments.Length != 3)
                        {
                            await Console.Error.WriteLineAsync("Invalid command usage.");
                            continue;
                        }
        
                        await connection.Auth(splitArguments[0], splitArguments[1], splitArguments[2]);
        
                        break;
                    case "/rename":
                        if (arguments == null)
                        {
                            await Console.Error.WriteLineAsync("Invalid command usage.");
                            continue;
                        }
                        
                        var possibleDisplayName = arguments[1].ToString();
                        
                        if (possibleDisplayName.ToCharArray().Any(character => character < 33 || character > 126))
                        {
                            await Console.Error.WriteLineAsync("Invalid display name.");
                            continue;
                        }
                        
                        connection.Rename(possibleDisplayName);
                    
                        break;
                    default:
                        await Console.Error.WriteLineAsync("Invalid command.");
                        break;
                }
            }
        }
    }
}