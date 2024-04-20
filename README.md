<div align="center">
  <img src="https://raw.githubusercontent.com/IanWold/PlanningPoker/main/logo.png" height="150">
  <h1>FreePlanningPoker.io</h1>
  <a href="https://freeplanningpoker.io"><img alt="Website" src="https://img.shields.io/website?url=https%3A%2F%2Ffreeplanningpoker.io&style=flat-square"></a>
  <a href="https://github.com/IanWold/PlanningPoker/issues?q=is%3Aopen+is%3Aissue+label%3A%22good+first+issue%22"><img alt="GitHub Issues or Pull Requests by label" src="https://img.shields.io/github/issues/ianwold/planningpoker/good%20first%20issue?style=flat-square"></a>
</div>

This is Free Planning Poker, a free tool for software teams to do "planning poker" exercises to estimate the difficulty and length of development tasks. You can probably use it for other purposes if you need, too. It's always going to be free, without limits.

Note that I've just started development on this so some documentation and whatnot is a WIP as this gets set up!

# Running Locally

The ideal scenario is that you can "clone and go" without much (if any) work, but there's a couple steps you need right now:

1. [Fork](https://github.com/IanWold/PlanningPoker/fork) and clone this repo
2. Download and install the [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
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

# Deploying

You can deploy this project yourself without much fuss. I recommend using [Railway](https://railway.app/), my favorite cloud provider for simple apps (heck, even some complicated scenarios are probably fine here).

In future I want to add some documentation around deploying on Docker, and since this is a .NET app I could include Azure Services documentation easily.

## Via Railway

_(See also my guide on [deploying ASP and Blazor apps on Railway](https://ian.wold.guru/Posts/deploying_aspdotnet_7_projects_with_railway.html))_

1. [Fork](https://github.com/IanWold/PlanningPoker/fork) and clone this repo
2. Create an account at [Railway](https://railway.app)
3. Create a [new project](https://docs.railway.app/guides/projects), and [add a Redis instance](https://docs.railway.app/guides/redis) to it
4. Add a [new service](https://docs.railway.app/guides/services) from your cloned GitHub repo (Railway will handle building and all)
5. Under the Settings for this service, use the following as your Custom Start Command for Deploy: `./out/PlanningPoker.Server`
6. Add your Redis connection string as an environment variable: `ConnectionStrings__Redis` (Use Railway's [reference variables](https://docs.railway.app/guides/variables#reference-variables) to make this easy)
7. Add two environment variables required for .NET:

```env
CONTENT_ROOT_PATH=./
NIXPACKS_CSHARP_SDK_VERSION=8.0
```

Now you should be good to go! Railway can [provide a domain name](https://docs.railway.app/guides/public-networking#railway-provided-domain) for your instance of FreePlanningPoker so you can use it.

Note that while you technically can deploy this without Redis, I don't recommend it since the in-memory store is not thread safe. If you want to make it thread safe I'd be more than happy to entertain that PR!

In future I'll be adding some of these settings to a Railway config file in the repo, eliminating the need for a couple of these steps.

# Developing

The client is a Blazor WASM SPA, the server is ASP and they communicate exclusively over SignalR (websockets). The server uses Redis as a backplane for SignalR and to store active sessions - this allows the server to scale horizontally.

<a href="https://link.excalidraw.com/readonly/NDvp574BNGntF6oGc3Cg?darkMode=true"><img src="https://raw.githubusercontent.com/IanWold/PlanningPoker/main/architecture.png"></a>

## Server

The SignalR communication is defined by two interfaces in the `PlanningPoker` project: [ISessionHub](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker/ISessionHub.cs) defines client-to-server communication (some of which does require a round trip) and [ISessionHubClient](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker/ISessionHubClient.cs) defines server-to-client communication (none of which requires a round trip; this must be asynchronous communication).

The logic for the server methods is in [SessionHub](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Server/SessionHub.cs) in the `Server` project. This class contains the _very minimal_ business rules and the scheme of notifying clients of changes through `ISessionHubClient`. Clients are grouped by session id, and only clients in a session will receive notifications for it. One future goal is to separate the DAL from this class so that a separate in-memory DAL can be implemented for easier local debugging.

State is kept by one of the two classes implementing [IStore](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Server/IStore.cs): either [InMemoryStore](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Server/InMemoryStore.cs) or [RedisStore](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Server/RedisStore.cs). The former is used for local debugging scenarios where Redis isn't strictly needed, while the latter is used for production deployments and any networking-related debugging and testing.

If you are adding a method on the server for the client to call, you'll update `ISessionHub`, implement the server logic in `SessionHub` and the store classes, then you'll update the client's `SessionState` to call it (see below). If you're adding a method on the client to call, you'll update `ISessionHubClient`, implement the client logic in `SessionState` (see below), then you'll update the server's `SessionHub` to call down through that method. Everything is strongly-typed by these interfaces on both the client and server, keeping you from needing to using magic strings.

Configuration and dependency injection are all set up in [Program](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Server/Program.cs); there's really not a lot there.

## Redis

Session data is stored in Redis across several keys to eliminate or minimize race conditions. The keys and their values are:

* `{sessionId}` (guid): Hash with values "Title" and "State".
* `{sessionId}:participants`: List with values being the IDs of the participants in the session.
* `{sessionId}:participants:{participantId}`: Hash with values "Name", "Points", and "Stars".

All entries associated with a session are removed from Redis when the last participant leaves the session.

## Client

There's two main files to care about: [SessionState](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Client/SessionState.cs) and [SessionPage](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Client/Pages/SessionPage.razor). The `SessionState` class keeps the state for the user and their session, and handles commands from the UI and notifications from the server which mutate state. As such, it also maintains the SignalR connection and the navigation in the app (this is trivial, that's just moving from the homepage to the session on creation). When the state mutates, the `OnStateChanged` event is raised.

`SessionState` implements `ISessionHubClient` and keeps an instance of `ISessionHub`, which fulfil the SignalR communication requirements. These are set up in `EnsureInitialized`, and torn down in `LeaveAsync`. Note that `EnsureInitialized` uses some [fancy source generation](https://github.com/IanWold/PlanningPoker/blob/main/PlanningPoker.Client/HubConnectionExtensions.cs) - the [package for this](https://github.com/dotnet/aspnetcore/tree/main/src/SignalR/clients/csharp/Client.SourceGenerator/src) _is_ from Microsoft, though it's undocumented and hasn't been updated in two years. If you dig enough online, you'll find [Kristoffer Strube's post](https://kristoffer-strube.dk/post/typed-signalr-clients-making-type-safe-real-time-communication-in-dotnet/) about them. When adding server functionality, this is the only file you need to change unles the functionality you're adding requires new UI components.

`SessionPage` is the user interface for almost the entire application. The user will first create a session on the homepage (`Index.razor`) but then all the work in the session is done on this page. This page listens to the `OnStateChanged` event from the state and calls `StateHasChanged` on itself when it receives that event. This is a potential area for improvement - these could be broken into components to perform more granular updates. As it stands, this is a very small application and having one page is, in its own way, easy. Probably time to start breaking it up though.

There are several components for individual UI elements in the `Components` folder. These are all referenced from `SessionPage`.

# Contributing

Please do! I think the above gives a fair quick overview of the project structure and how to add some features. I've got several [good first issues](https://github.com/IanWold/PlanningPoker/issues) and I'm always happy to discuss suggestions for what to include, modify, etc.

If you would like to champion an issue, please leave a comment saying you'd like to - I'll assign the issue to you and I'll be happy to clarify any questions.

I don't have formal code standards on this proejct yet; it's quite small and young. I ask that your code be kept minimal, tidy, and in-keeping with the code that's already here. In future as the application solidifies then a more defined coding and architectural standard will probably emerge - I find that a codebase will generally reveal its own standards over time and I prefer allowing that process rather than imposing a (probably wrong) idea on the codebase from the start.
