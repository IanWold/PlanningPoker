# FreePlanningPoker

This is Free Planning Poker, a free tool for software teams to do "planning poker" exercises to estimate the difficulty and length of development tasks. You can probably use it for other purposes if you need, too. It's always going to be free, without limits.

Note that I've just started development on this so some documentation and whatnot is a WIP as this gets set up!

# Architecture

The client is a Blazor WASM SPA, the server is ASP and they communicate exclusively over SignalR (websockets). The server uses Redis as a backplane for SignalR and to store active sessions - this allows the server to scale horizontally.

The deployed isntance is hosted on Railway, which hosts the server in Docker.
