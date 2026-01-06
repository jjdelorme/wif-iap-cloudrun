# Implementation Plan: Cloud Run with IAP & WIF Authentication

## Project Goal
Build a robust "Hello World" demonstration application to validate the architectural pattern of protecting a Cloud Run service using Identity-Aware Proxy (IAP). The application consists of a single container hosting both a static frontend and a backend API.

The application will demonstrate:
1.  **Secure Backend**: Verifying the `X-Goog-Iap-Jwt-Assertion` header to ensure requests are authenticated.
2.  **Resilient Frontend**: Handling IAP session expiration (the "Monday Morning" problem) by detecting 302 Redirects or 401 errors on API calls and triggering a page reload to re-authenticate.
3.  **Workforce Identity Federation (WIF)**:  Final integration allowing users to authenticate via an external Identity Provider (e.g., Azure Entra ID) instead of standard Google accounts.

## Phase 1: Application Development
We will build a Node.js/Express application.

1.  **Project Initialization**
    *   Create project structure.
    *   Initialize `package.json` with `express` (web server) and `google-auth-library` (for JWT verification).

2.  **Backend Implementation (`server.js`)**
    *   **Express Setup**: Configure the app to serve static files from a `public/` directory.
    *   **Identity Endpoint (`/api/data`)**:
        *   Create a route that inspects the `X-Goog-Iap-Jwt-Assertion` header.
        *   **Verification Logic**: Use `google-auth-library` to verify the JWT signature and audience claim (crucial for security).
        *   **Response**: Return a JSON object containing the user's identity (email/subject) extracted from the token.
    *   **Local Development Handling**: Add logic to mock the IAP header when running locally so development doesn't require a full IAP tunnel.

3.  **Frontend Implementation (`public/` directory)**
    *   **UI (`index.html`)**: A minimal interface with a "Fetch Protected Data" button and a pre/code block to display results.
    *   **Client Logic (`client.js`)**:
        *   Implement a `fetch` wrapper or interceptor.
        *   **"Monday Morning" Handler**: Logic to detect if an API call fails due to an expired IAP session.
            *   *Detection*: Check for HTTP 401/403 status OR if the response `Content-Type` is `text/html` (which indicates IAP returned a login page instead of JSON).
            *   *Action*: Call `window.location.reload()` to force the browser to perform a top-level navigation, allowing IAP to redirect the user to the IDP login page.

4.  **Containerization**
    *   Create a `Dockerfile` to package the Node.js application.

## Phase 2: Deployment & Standard IAP Setup
Deploy the application and secure it with standard Google Account IAP first.

1.  **Deploy to Cloud Run**
    *   Build and deploy the container.
    *   **Security setting**: `--no-allow-unauthenticated` (Require IAM authentication).

2.  **Enable Identity-Aware Proxy (IAP)**
    *   Configure the **OAuth Consent Screen** (Internal).
    *   Enable IAP on the Cloud Run backend service.
    *   **Access Control**: Add a standard Google Account/Group to the "IAP-secured Web App User" role to verify the app works with standard Google authentication.

3.  **Verify Core Application**
    *   Visit the URL -> Redirect to Google Login -> App Loads.
    *   Click "Fetch Protected Data" -> Verify JSON response confirms identity.
    *   (Optional) Test session expiry logic if possible.

## Phase 3: Workforce Identity Federation (WIF) Configuration
**Deferred Final Step**: Switch authentication from Google Accounts to the external IDP.

1.  **Configure IAP Settings for WIF**
    *   Create an `iap_settings.yaml` file.
    *   Define the `workforce_pools` configuration to point to your specific WIF pool.
    *   Apply settings via `gcloud iap settings set ...`.

2.  **Update Access Permissions**
    *   Grant `roles/iap.httpsResourceAccessor` to the WIF principals (e.g., `principalSet://iam.googleapis.com/...`).

3.  **Final Verification**
    *   Access the app.
    *   Verify redirection to the External IDP (Azure).
    *   Confirm the application still functions correctly with the new identity token structure.