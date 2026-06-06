# Security Policy

FreePlanningPoker.io inherently has very little in terms of security requirements. It would be surprising if any truly sensitive data was ever handled on the app. For any organizatinos that take a paranoid approach to security, FreePlanningPoker can always be deployed on their private network.

Nonetheless, it's good practice for applications to maintain a reasonable level of security. FreePlanningPoker.io should not expose _any_ unencrypted user-entered data to users who don't have the security key for a session, and _no_ unencrypted user-entered data should be kept on the server. Further, _no_ data should live on the server longer than 24 hours. The approach FreePlanningPoker.io uses for encryption is [detailed on my blog](https://ian.wold.guru/Posts/end_to_end_encryption_witn_blazor_wasm.html).

Any security vulnerabilities found should be [reported directly to me](https://ian.wold.guru/connect.html); any private manner of communication is fine but email is preferred.
