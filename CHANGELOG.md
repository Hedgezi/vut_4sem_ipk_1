## Implemented functionality
The project implements the IPK24-CHAT protocol. The protocol is divided into two variants: UDP and TCP, which are implemented in separate namespaces. Proper error handling is implemented, and the client can handle all of the needed commands and messages. The client can also handle the Ctrl+C signal to close the application by sending a `BYE` message to the server.

## Known limitations
I'm not aware of any limitations at the moment.  
The only possible problem could be that when client receives an message with `BYE` command or message with some error, it properly closes the connection and immediately exits the application, which I'm not sure is the desired behavior.