# Cloud Run with IAP & Workforce Identity Federation

This project demonstrates how to protect a Google Cloud Run service using Identity-Aware Proxy (IAP) and eventually integrate Workforce Identity Federation (WIF).

## Project Goal

Build a robust "Hello World" application to validate:
1.  **Secure Backend**: Verifying `X-Goog-Iap-Jwt-Assertion` headers.
2.  **Resilient Frontend**: Handling IAP session expiration (the "Monday Morning" problem) with automatic re-authentication.
3.  **Workforce Identity Federation**: Authenticating via external Identity Providers (e.g., Azure Entra ID).

## Prerequisites

*   Google Cloud Project with Billing enabled.
*   `gcloud` CLI installed and authenticated.
*   Required APIs enabled:
    *   `run.googleapis.com`
    *   `iap.googleapis.com`
    *   `artifactregistry.googleapis.com`
    *   `cloudbuild.googleapis.com`
    *   `compute.googleapis.com`
    *   `iam.googleapis.com`

## Local Development

The application includes mock logic for local development.

1.  Install dependencies:
    ```bash
    npm install
    ```
2.  Start the server:
    ```bash
    npm start
    ```
3.  Visit `http://localhost:8080`. The app will simulate an authenticated user.

## Deployment & IAP Configuration

### 1. Deploy to Cloud Run

You can choose to deploy either the **Node.js** or the **.NET** backend. Both share the same frontend in the `public/` directory.

**Note:** Use `gcloud beta` to access the `--iap` flag, which automatically enables IAP for the service.

#### Option A: Deploy Node.js Backend
```bash
gcloud beta run deploy wif-iap-cloudrun \
  --source . \
  --dockerfile Dockerfile.node \
  --region us-central1 \
  --no-allow-unauthenticated \
  --iap
```

#### Option B: Deploy .NET Backend
```bash
gcloud beta run deploy wif-iap-cloudrun \
  --source . \
  --dockerfile Dockerfile.dotnet \
  --region us-central1 \
  --no-allow-unauthenticated \
  --iap
```

### 2. Configure IAP Access (Critical Step)

For IAP to function correctly with Cloud Run, the IAP Service Agent must have permission to invoke your Cloud Run service.

**Retrieve Project Number:**
```bash
PROJECT_NUMBER=$(gcloud projects describe $(gcloud config get-value project) --format="value(projectNumber)")
```

**Grant `roles/run.invoker` to the IAP Service Agent:**
```bash
gcloud run services add-iam-policy-binding wif-iap-cloudrun \
  --region=us-central1 \
  --member=serviceAccount:service-${PROJECT_NUMBER}@gcp-sa-iap.iam.gserviceaccount.com \
  --role=roles/run.invoker
```

### 3. Enable IAP

1.  Go to the [Identity-Aware Proxy](https://console.cloud.google.com/security/iap) page in the Google Cloud Console.
2.  Configure your **OAuth Consent Screen** (Internal type is recommended for testing).
3.  Find your Cloud Run backend service in the list and toggle the switch to **Enable IAP**.

### 4. Grant Access to Users

Grant the **IAP-secured Web App User** role (`roles/iap.httpsResourceAccessor`) to users or groups who need access.

```bash
gcloud projects add-iam-policy-binding $(gcloud config get-value project) \
  --member="user:YOUR_EMAIL@example.com" \
  --role="roles/iap.httpsResourceAccessor"
```

## Workforce Identity Federation (WIF)

*(Phase 3 - Implementation Pending)*

Future steps to enable WIF:
1.  Configure `iap_settings.yaml` with your Workforce Pool.
2.  Apply settings via `gcloud iap settings set`.
3.  Grant `roles/iap.httpsResourceAccessor` to WIF principals.

```