<div align="center">
  
<img src="https://raw.githubusercontent.com/IanWold/PlanningPoker/main/logo.png" height="150">

# FreePlanningPoker.io

<a href="https://freeplanningpoker.io"><img alt="Website" src="https://img.shields.io/website?url=https%3A%2F%2Ffreeplanningpoker.io&style=for-the-badge"></a>
<a href="https://hub.docker.com/r/ianwold/free-planning-poker"><img alt="Docker Image Version" src="https://img.shields.io/docker/v/ianwold/free-planning-poker?style=for-the-badge&label=dockerhub"></a>
<a href="https://github.com/IanWold/PlanningPoker/issues?q=is%3Aopen+is%3Aissue+label%3A%22good+first+issue%22"><img alt="GitHub Issues or Pull Requests by label" src="https://img.shields.io/github/issues-search?query=repo%3Aianwold%2Fplanningpoker%20is%3Aissue%20state%3Aopen%20label%3A%22good%20first%20issue%22&style=for-the-badge&label=Good%20First%20Issues&color=yellow"></a>

Fast and easy planning poker sessions for your whole team: free, secure, and open-source!

Always Free ♣ Unlimited Sessions ♠ Unlimited Participants

[FreePlanningPoker.io](https://freeplanningpoker.io) •
[Deploy Yourself](#deploying) •
[Contribute](#contributing)

</div>

---

This is Free Planning Poker, a free tool for software teams to do "planning poker" exercises to estimate the difficulty and length of development tasks. You can probably use it for other purposes if you need, too. It's always going to be free, without limits.

# Running Locally

The ideal scenario is that you can "clone and go" without much (if any) work, but there's a couple steps you need right now:

1. [Fork](https://github.com/IanWold/PlanningPoker/fork) and clone this repo
2. Download and install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
3. I recommend using VSCode with the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit&WT.mc_id=dotnet-35129-website)

You should be good to go now - hit F5 and watch it run! By default it will use an in-memory store to keep state. This store is _not_ thread safe; in order to get thread safety (and to allow SignalR to use a backplane) you'll need to provide a connection string for Redis. However, the in-memory store is fast and ideal for local debugging scenarios.

## Running with Redis

1. You will need access to _some_ deployment of Redis. [Redis' Quick Start docs](https://redis.io/docs/latest/get-started/) can help you here.
2. Add your Redis connection string in [appsettings](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Server/appsettings.Development.json):

```json
"ConnectionStrings": {
    "Redis": "<your-connection-string>"
}
```

The application will see your connection string and use the Redis store instead of the in-memory store, and it will use Redis as a backplane for SignalR.

In future I want to look into Docker environments to be able to remove standing up your own Redis as being a burden.

## Running with Docker

FreePlanningPoker comes with a standalone [Dockerfile](https://github.com/IanWold/PlanningPoker/blob/main/Dockerfile) that you can run in Docker.

# Deploying

You can deploy this project yourself without much fuss. I recommend using [Railway](https://railway.app/), my favorite cloud provider for simple apps (heck, even some complicated scenarios are probably fine here).

In future I want to add some documentation around deploying on Docker, and since this is a .NET app I could include Azure Services documentation easily.

## Via Railway

_(See also my guide on [deploying ASP and Blazor apps on Railway](https://ian.wold.guru/Posts/deploying_aspdotnet_7_projects_with_railway.html))_

1. [Fork](https://github.com/IanWold/PlanningPoker/fork) and clone this repo
2. Create an account at [Railway](https://railway.app)
3. Create a [new project](https://docs.railway.app/guides/projects), and [add a Redis instance](https://docs.railway.app/guides/redis) to it
4. Add a [new service](https://docs.railway.app/guides/services) from your cloned GitHub repo (Railway will handle building and all)
5. Add your Redis connection string as an environment variable: `ConnectionStrings__Redis` (Use Railway's [reference variables](https://docs.railway.app/guides/variables#reference-variables) to make this easy)

Now you should be good to go! Railway can [provide a domain name](https://docs.railway.app/guides/public-networking#railway-provided-domain) for your instance of FreePlanningPoker so you can use it.

Note that while you technically can deploy this without Redis, I don't recommend it since the in-memory store is not thread safe. If you want to make it thread safe I'd be more than happy to entertain that PR!

In future I'll be adding some of these settings to a Railway config file in the repo, eliminating the need for a couple of these steps.

## Via Docker

FreePlanningPoker comes with a standalone [Dockerfile](https://github.com/IanWold/PlanningPoker/blob/main/Dockerfile) that you can use to deploy to any containerized environment.

## Via Azure

_This section TBD_.

If you're hoping to contribute, this would be a good first issue to [add documentation for this](https://github.com/IanWold/PlanningPoker/issues/26)! Realistically, if you have an Azure subscription you should be able to click the Publish button in Visual Studio and send it up in a new App Service.

# Developing

The web client is a Blazor WASM SPA, the server is ASP and they communicate exclusively over SignalR (websockets). The server uses Redis as a backplane for SignalR and to store active sessions - this allows the server to scale horizontally.

<a href="https://link.excalidraw.com/readonly/NDvp574BNGntF6oGc3Cg?darkMode=true"><img src="https://raw.githubusercontent.com/IanWold/PlanningPoker/main/architecture.png"></a>

## Server

The SignalR communication is defined by two interfaces in the `PlanningPoker` project: [IServer](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker/IServer.cs) defines client-to-server communication (some of which does require a round trip) and [IClient](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker/IClient.cs) defines server-to-client communication (none of which requires a round trip; this must be asynchronous communication).

The logic for the server methods is in [SessionHub](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Server/SessionHub.cs) in the `Server` project. This class contains the _very minimal_ business rules and the scheme of notifying clients of changes through `IClient`. Clients are grouped by session id, and only clients in a session will receive notifications for it.

State is kept by one of the two classes implementing [IStore](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Server/IStore.cs): either [InMemoryStore](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Server/InMemoryStore.cs) or [RedisStore](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Server/RedisStore.cs). The former is used for local debugging scenarios where Redis isn't strictly needed, while the latter is used for production deployments and any networking-related debugging and testing.

If you are adding a method on the server for the client to call, you'll update `IServer`, implement the server logic in `SessionHub` and the store classes, then you'll update the client's `Client` class to call it (see below). If you're adding a method on the client to call, you'll update `IClient`, implement the client logic in `Client` and `SessionStore` (see below), then you'll update the server's `SessionHub` to call down through that method. Everything is strongly-typed by these interfaces on both the client and server, keeping you from needing to using magic strings.

Configuration and dependency injection are all set up in [Program](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Server/Program.cs); there's really not a lot there.

## Redis

Session data is stored in Redis across several keys to eliminate or minimize race conditions. The keys and their values are:

* `{sessionId}` (guid): Hash with values "Title" and "State".
* `{sessionId}:points`: List with values being the point options available in the session.
* `{sessionId}:participants`: List with values being the IDs of the participants in the session.
* `{sessionId}:participants:{participantId}`: Hash with values "Name", "Points", and "Stars".

All entries associated with a session are removed from Redis when the last participant leaves the session. For extra safety, the keys all have a 24-hour TTL.

## Client

The Client is separated into two pieces - a logical client in [PlanningPoker.Client](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Client) and the web (Blazor) view in [PlanningPoker.Client.Web](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Client.Web). The logical client is split by responsibility rather than one big class:

* [SessionStore](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Client/SessionStore.cs) holds the session data a UI needs to render - the current `Session`, `Self`/`Others`, `ShowShareNotification`, etc. - and raises `Changed` whenever any of it updates. Nothing outside `PlanningPoker.Client` can write to it directly; its state only changes through its own methods.
* [ToastStore](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Client/ToastStore.cs) holds the transient notification toasts shown in the UI, independently of session data, with its own `Changed` event.
* [Client](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Client/Client.cs) is the piece that actually knows about SignalR - it implements `IClient`, keeps an instance of `IServer`, and exposes the methods a UI calls to act (`CreateAsync`, `UpdatePoints`, etc). It writes into `SessionStore`/`ToastStore` as a side effect of handling server callbacks or fulfilling those calls, but exposes nothing to read itself - components should consult the stores for state and call `Client` only to perform actions.

Platform-specific behavior (building the hub URL, what to do when a session is created or closed, any JS interop needed at startup) is captured by the [ISessionTransport](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Client/ISessionTransport.cs) interface, which `Client` takes as a constructor dependency rather than through subclassing. This keeps `Client` itself free of any browser-specific dependency and lets a different UI (or the integration tests) supply its own transport.

`Client`'s connection is set up in `EnsureInitialized`, and torn down in `LeaveAsync`. When adding server functionality, you'll typically only need to change `Client` (for the SignalR/actions side) and `SessionStore` (if the change adds or updates data a UI needs to read) unless it also requires new UI components.

On the Blazor client, there's two main files to care about: [WebSessionTransport](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Client.Web/WebSessionTransport.cs) (the browser's `ISessionTransport` implementation) and [SessionPage](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Client.Web/Pages/SessionPage.razor).

`SessionPage` is the user interface for almost the entire application. The user will first create a session on the homepage (`Index.razor`) but then all the work in the session is done on this page. Components inject `SessionStore` (and `ToastStore`, where relevant) to read state and subscribe to `Changed`, and inject `Client` separately to perform actions. Several components for the UI are broken out into separate Razor components in [the Components directory](https://github.com/IanWold/PlanningPoker/tree/main/PlanningPoker.Client.Web/Components).

Dark/light color mode is handled directly through JS in [index.html](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Client.Web/wwwroot/index.html). E2E encryption is implemented there too, but `Client` never calls into JS for it directly - it depends on the [IEncryptionService](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Client/IEncryptionService.cs) interface, which [EncryptionService](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Client.Web/EncryptionService.cs) implements via JS interop. This keeps the logical client itself free of any browser-specific dependency, so a non-Blazor client could supply its own `IEncryptionService` instead. I wrote a [blog post on E2E encryption](https://ian.wold.guru/Posts/end_to_end_encryption_witn_blazor_wasm.html) which covers the implementation used here.

## Testing

Integration tests live in [PlanningPoker.IntegrationTests](https://github.com/IanWold/PlanningPoker/tree/main/PlanningPoker.IntegrationTests). They deliberately drive the app only through `Client`, `SessionStore`, and `ToastStore` (via a `TestSessionTransport` implementation of `ISessionTransport`, the same pattern `WebSessionTransport` follows) rather than reaching into `SessionHub` or the store classes directly - the goal is to test the behavior a participant actually sees, so the suite doesn't need to change every time something internal gets refactored.

Each behavior is written once and run in up to three modes: against `InMemoryStore`, against `RedisStore` with a single server instance, and (for tests with more than one participant) against `RedisStore` with two server instances sharing one Redis, to prove the SignalR backplane actually relays messages between them. The Redis-backed runs spin up a real Redis container via [Testcontainers](https://dotnet.testcontainers.org/), so you'll need Docker running locally to execute the full suite - `dotnet test` will otherwise fail on those specific tests while the `InMemoryStore` ones still pass fine.

# Contributing

Please do! I think the above gives a fair quick overview of the project structure and how to add some features. I've got several [good first issues](https://github.com/IanWold/PlanningPoker/issues) and I'm always happy to discuss suggestions for what to include, modify, etc.

If you would like to champion an issue, please leave a comment saying you'd like to - I'll assign the issue to you and I'll be happy to clarify any questions.

I don't have formal code standards on this proejct yet; it's quite small and young. I ask that your code be kept minimal, tidy, and in-keeping with the code that's already here. In future as the application solidifies then a more defined coding and architectural standard will probably emerge - I find that a codebase will generally reveal its own standards over time and I prefer allowing that process rather than imposing a (probably wrong) idea on the codebase from the start.
