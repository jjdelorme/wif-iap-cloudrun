const express = require('express');
const { OAuth2Client } = require('google-auth-library');

const app = express();
const port = process.env.PORT || 8080;

// Serve static files from the 'public' directory
app.use(express.static('public'));

// IAP JWT Verification
const oAuth2Client = new OAuth2Client();

const verifyIapToken = async (iapJwt) => {
  try {
    // Decode the token to get the audience (to allow dynamic verification)
    // NOTE: In production, you should validate that 'aud' matches your specific Service/Client ID.
    const [header, payloadBase64, signature] = iapJwt.split('.');
    const payload = JSON.parse(Buffer.from(payloadBase64, 'base64').toString());
    const audience = payload.aud;

    // Fetch IAP public keys
    // verifySignedJwtWithCertsAsync expects a map of key IDs to PEMs.
    // getIapPublicKeys returns { pubkeys: { kid: pem, ... }, res: ... }
    const iapResponse = await oAuth2Client.getIapPublicKeys();
    const iapPublicKeys = iapResponse.pubkeys;
    
    // Verify using verifySignedJwtWithCertsAsync to support IAP issuer
    const ticket = await oAuth2Client.verifySignedJwtWithCertsAsync(
        iapJwt,
        iapPublicKeys,
        audience, 
        ['https://cloud.google.com/iap']
    );
    
    return ticket.getPayload();
  } catch (error) {
    console.error('Error verifying IAP token:', error);
    throw error;
  }
};

app.get('/api/data', async (req, res) => {
  const iapJwt = req.header('X-Goog-Iap-Jwt-Assertion');

  if (iapJwt) {
    try {
      const payload = await verifyIapToken(iapJwt);
      // In IAP, 'sub' is the unique user ID, and 'email' is often present.
      // For WIF, the subject format might be different.
      res.json({
        message: 'Authenticated via IAP',
        user: payload.email || payload.sub,
        raw_payload: payload
      });
    } catch (error) {
      res.status(401).json({ error: 'Invalid IAP Token' });
    }
  } else {
    // Local Development / Mocking
    // If no header is present, we check if we are in a production-like environment.
    // Ideally, Cloud Run always sets this header if IAP is enabled.
    // For local dev, we assume we are authenticated as a mock user.
    console.log('No IAP header found. Assuming local development.');
    res.json({
      message: 'Local Development Mode (Mocked IAP)',
      user: 'local-dev-user@example.com',
      raw_payload: {
        sub: 'mock-subject-id',
        email: 'local-dev-user@example.com',
        iss: 'mock-issuer'
      }
    });
  }
});

app.listen(port, () => {
  console.log(`Server listening on port ${port}`);
});
