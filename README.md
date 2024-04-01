# IPK Project 1: Client for a chat server using IPK24-CHAT protocol
Author: Mykola Vorontsov (xvoron03)  
License: AGPL-3.0  

## Theory summary
This is an implementation of a client for the `IPK24-CHAT` protocol as specified by its [specification](https://git.fit.vutbr.cz/NESFIT/IPK-Projects-2024/src/branch/master/Project%201).  
I've used C# programming language and .NET 8 to implement this project. 
For parallel processing, I've used the asynchronous programming model, using C#'s `async` and `await`.
The core idea for client is using two parallel-working Tasks: one for receiving messages from the server and the other for processing user input and sending messages to the server.



## Project structure
The project is divided into four general namespaces (folders) and one separate project with unit tests.

### Namespace `vut_ipk1`
* `Program.cs` is the entry point of the application. It consists of:
  1. Parsing command line arguments using `CommandLineOptions` library.
  2. Handling the Ctrl+C signal to close the application by sending a `BYE` message to the server.
  3. Resolving the server's IP address using the `Dns` class.
  4. Launching the client based on the protocol specified in the command line arguments and user input processor.
* `CommandLineOptions.cs` is a class that holds parsed command line arguments, using the `CommandLineParser` library.  
* `Makefile`: has targets for building and cleaning the project.  
* `README.md`: documentation (this file).  
* `LICENSE`: license file with AGPL-3.0 license.  

### Namespace `vut_ipk1.Common`
This namespace contains classes that are used in multiple parts of the project.
* Folder `Enums` contains enums used in the project: `MessageType.cs` for message types of IPK24-CHAT protocol, `FsmState.cs` for states of the finite state machine, and `ErrorMessage.cs` for error messages.
* Folder `Interfaces` contains only one interface `IConnection` that is implemented by `TcpConnection` and `UdpConnection` classes.
* Folder `Structures` contains only one structure `FixedSizeQueue.cs` that is used primarily for its FILO structure.
One of the uses is storing message IDs in `UdpConnection` where the oldest message ID is removed when the queue is full, so it won't overflow.
* `UserInputProcessing.cs` is a class that processes user input and sends messages to the server.
The passed argument is a `IConnection` interface that is used to send messages to the server, invoking proper methods based on the protocol.

### Namespace `vut_ipk1.Udp`
This namespace contains classes that are used for the UDP variant of the IPK24-CHAT protocol.
* [`UdpConnection.cs`](#udp-connection) is a class that implements the `IConnection` interface and is used to send messages to the server using UDP. The whole process of communication is described [here](#udp-connection).
* `Messages/UdpMessageGenerator.cs` is a class that generates messages for the UDP variant of the IPK24-CHAT protocol.
* `Messages/UdpMessageParser.cs` is a class that parses messages for the UDP variant of the IPK24-CHAT protocol.

### Namespace `vut_ipk1.Tcp`
This namespace contains classes that are used for the TCP variant of the IPK24-CHAT protocol.
* [`TcpConnection.cs`](#tcp-connection) is a class that implements the `IConnection` interface and is used to send messages to the server using TCP. The whole process of communication is described [here](#tcp-connection).
* `Messages/TcpMessageGenerator.cs` is a class that generates messages in ASCII encoding for the TCP variant of the IPK24-CHAT protocol.
* `Messages/TcpMessageParser.cs` is a class that parses messages and controls it with proper regexes for the TCP variant of the IPK24-CHAT protocol.
* `Facades/TcpMessageReceiver.cs` is a class that receives messages from the server and processes them.
It receives messages over the stream and divides it into separate messages based on the `\r\n` delimiter and serves it one by one to the client.

### Project `UnitTests`
This project contains unit tests for the project.
* `Udp/UdpMessageGeneratorUnitTests.cs` contains unit tests for the `UdpMessageGenerator` class.


## [UDP connection](#udp-connection)

### AUTH message (JOIN works the same way)
![UDP Auth message Sequence Diagram](Doc/imgs/ipk_udp_auth.svg "UDP Auth message Sequence Diagram")

### Send message
![UDP Send message Sequence Diagram](Doc/imgs/ipk_udp_sendmsg.svg "UDP Send message Sequence Diagram")


## [TCP connection](#tcp-connection)

### AUTH message (JOIN works the same way)
![TCP Auth message Sequence Diagram](Doc/imgs/ipk_tcp_auth.svg "TCP Auth message Sequence Diagram")

### Send message
![TCP Send message Sequence Diagram](Doc/imgs/ipk_tcp_sendmsg.svg "TCP Send message Sequence Diagram")


## Extra functionality
There is no extra functionality implemented in this project.


## Testing

### UDP
All testing of UDP part was done using Wireshark, Public Reference Server and Unit Tests.  
The main reason for using Wireshark was to verify that the messages are sent correctly and in the right format.  
The unit tests ensured that the messages are parsed correctly. Especially parsing of strings from messages was tested.

### TCP
All testing of TCP part was done using Public Reference Server. I had no problems implementing the TCP part, so I didn't need to use Wireshark to verify the messages.


## Bibliography
1. [Project 1 - IPK-Projects-2024](https://git.fit.vutbr.cz/NESFIT/IPK-Projects-2024/src/branch/master/Project%201)
