This project contains the server, which itself serves the client at `/`. The server exclusively transacts over websockets through a single SignalR `Hub`. The amount of presentation-layer overhead from SignalR is extremely minimal, so the hub also acts as the business layer. The only other layer is the data access layer, which can be configured to be either in memory or Redis.

### SessionHub

`SessionHub` is the SignalR hub, both the presentation and business layers. The hub expects that clients generate their own GUID id, and set the query string parameter `participantId` when connecting. The hub has a custom `UserIdProvider` to read this query param and set the hub's `Context.UserIdentifier` to this value. The connection will be aborted if this value is not set.

The hub groups all of its connections by the id of the session to which they are connected, allowing messages to be sent within sessions. If the application is configured with a Redis connection string (environment variable `ConnectionStrings__Redis`) then SignalR will use Redis as a backplane, allowing groups to be shared across multiple instances of the server. If the server is run across multiple instances but no backplane is configured, the server will be unable to allow clients connecting to different servers to join the same session.

### Store

`IStore` defines the data access contract used by `SessionHub`; there are two implementations: `InMemoryStore` and `RedisStore`. The choice of which to use depends on whether the app is configured with a Redis connection string (environment variable `ConnectionStrings__Redis`): if one is present then `RedisStore` is used; otherwise `InMemoryStore` will be registered.

`InMemoryStore` keeps a dictinoary of sessions, relating session ids to their respective sessions. The logic here is quite straightforward.

`RedisStore` manages storing state in Redis, which is more complicated due to Redis' nature as a key-value database. This store uses the following keys to store various parts of data:

* `{sessionId}`: hash representing the session:
    * `Title`: string: the session title
    * `State`: string: the state (hidden/revealed)
* `{sessionId}:points`: list of strings (the point options available in the session)
* `{sessionId}:participants`: list of strings (the GUID ids of all the participants in the session)
* `{sessionId}:participants:{participantId}` hash representing an individual participant:
    * `Name`: string: the name
    * `Points`: string: the selected points option (or empty)
    * `Stars`: int: the number of stars

Most commands are dispatched to Redis with "fire and forget": the server does not wait for a response or confirmation of success. This is faster but more bittle; nobody's life will be hurt by this application but the maintainer has a financial incentive for it to consume fewer resources.
