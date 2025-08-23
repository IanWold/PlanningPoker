This project contains all of the shared code between the client and the server. This consists of the models and the SignalR interfaces.

### Models

To use FreePlanningPoker, an initial user will visit the homepage and enter their name, a name for their pointing session, and they will select a template to use for the default value of their points (Fibonacci, t-shirt sizes, etc). The `Session` model stores keeps the top-level state of a single session, including all participants, the point value options for the session, and the title. The (admittedly confusingly-named) "state" of the session (either hidden or revealed) is stored in this model as well.

The `Participant` model keeps all of the information for a single participant. Each participant gets a participant id, which is the connection id assigned by SignalR when the participant's client connects to the server. Apart from the id, this model tracks their name, the point value they have selected, and how many stars they've been given by other players.

### Interfaces

SignalR, as it is used in this repository, requires two interfaces - one interface for the server (to accept messages from the client), and one interface for the client (to accept messages from the server).

`IServer` defines commands for the client to send to the server, and `IClient` defines events for the server to trigger on the client; the style for this repository is to keep that call pattern. 
