# Issue Certificate API - SDK Reference Guide

> **Target Audience**: SDK Client Teams  
> **Protocol**: MQTT (x509 authentication)  
> **API Version**: Latest (from test execution 2026-01-30)

---

## 📋 Overview

The Issue Certificate API allows devices to request new certificates via MQTT. The flow is asynchronous:
1. Device publishes CSR request → receives `202 Accepted`
2. Backend processes request → device receives `200 OK` with certificate (or error)

---

## 🔌 MQTT Topics

| Direction | Topic Pattern |
|-----------|---------------|
| **Request** | `$iothub/credentials/POST/issueCertificate/?$rid={request_id}` |
| **Response** | `$iothub/credentials/res/{status_code}/?$rid={request_id}` |
| **Subscribe** | `$iothub/credentials/res/#` |

> ⚠️ **Important**: The `$rid` (request ID) parameter is **required** to receive responses.

---

## 📤 Request Format

```json
{
  "id": "<device_id>",
  "csr": "<base64_encoded_csr>",
  "replace": "<optional: request_id | *>"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | string | ✅ | Must match the authenticated device ID |
| `csr` | string | ✅ | Base64-encoded PKCS#10 CSR (max 8KB) |
| `replace` | string | ❌ | Cancel pending request: `*` (any) or specific `request_id` |

---

## 📥 Response Formats

### Success: 202 Accepted (Request Queued)
```json
{
  "correlationId": "5ecd03d5-8183-412b-9d6e-b95a7f36feb7",
  "operationExpires": "2026-01-31T12:52:20.565000000Z"
}
```

### Success: 200 OK (Certificate Issued)
```json
{
  "correlationId": "5ecd03d5-8183-412b-9d6e-b95a7f36feb7",
  "certificates": [
    "<base64_leaf_cert>",
    "<base64_intermediate_cert>",
    "<base64_root_cert>"
  ]
}
```

### Error Response Structure
```json
{
  "errorCode": 400004,
  "message": "Human-readable error message",
  "trackingId": "4A745D4B29A44D82BD5FE49D7F974C72-G2:-TimeStamp:...",
  "timestampUtc": "2026-01-31T00:49:39.467401528Z",
  "info": null | { ... }
}
```

---

## 🧪 Test Scenarios Reference

### 1️⃣ Payload Validation Errors

| # | Scenario | Request | Response | Error Code |
|---|----------|---------|----------|------------|
| 001 | **Empty Request Body** | `(empty)` | `400` | `400004` |
| | | | `"Issue certificate request payload is missing. Include a JSON payload with 'id' and 'csr' fields."` | |
| 002 | **Malformed JSON** | `{"id": "device", "csr":` (truncated) | `400` | `400006` |
| | | | `"cannot decode json format"` | |
| 003 | **JSON Array Instead of Object** | `["not", "an", "object"]` | `400` | `400004` |
| | | | `"Issue certificate request 'id' field does not match..."` | |

---

### 2️⃣ CSR Field Validation

| # | Scenario | Request CSR | Response | Error Code |
|---|----------|-------------|----------|------------|
| 004 | **Empty CSR** | `"csr": ""` | `400` | `400004` |
| | | | `"Issue certificate request 'csr' field is invalid or missing. Provide a valid base64-encoded certificate signing request."` | |
| 005 | **CSR Exceeds 8KB** | 10KB payload | `400` | `400004` |
| | | | `"Issue certificate request 'csr' field exceeds maximum allowed length. Reduce the CSR size."` | |
| 006 | **Invalid Base64** | `"csr": "not-valid-base64!!@@##"` | `400` | `400004` |
| | | | `"Issue certificate request 'csr' field is not valid base64. Ensure the CSR is properly base64-encoded."` | |
| 012 | **Malformed PKCS#10 (Backend)** | Valid base64, invalid CSR structure | `202` then `400` | `400037` |
| | | | `"Unable to complete the certificate request at this time..."` | |
| | | | `info.credentialMessage`: `"CSR did not pass verification"` | |
| | | | `info.credentialError`: `"400000"` | |

---

### 3️⃣ Identity Validation

| # | Scenario | Request | Response | Error Code |
|---|----------|---------|----------|------------|
| 007 | **Device ID Mismatch** | `"id": "wrong-device"` (auth as different device) | `400` | `400004` |
| | | | `"Issue certificate request 'id' field does not match the authenticated device ID. Use the same device ID used for authentication."` | |
| 008 | **Empty Device ID** | `"id": ""` | `400` | `400004` |
| | | | `"Issue certificate request 'id' field is invalid or missing. Provide the device ID of the authenticated device."` | |

---

### 4️⃣ Schema Validation

| # | Scenario | Request | Response | Error Code |
|---|----------|---------|----------|------------|
| 009 | **Unknown Field** | `{ "id": "...", "csr": "...", "unknownField": "value" }` | `400` | `400004` |
| | | | `"Issue certificate request payload contains an unknown field. Only 'id', 'csr', and 'replace' fields are allowed."` | |

---

### 5️⃣ Replace Field Validation

| # | Scenario | Request | Response | Error Code |
|---|----------|---------|----------|------------|
| 010 | **Invalid Format** | `"replace": "123"` (too short) | `400` | `400004` |
| | | | `"Issue certificate request 'replace' field has an invalid format. An exact request ID must be between 4 and 36 characters, only have alphanumeric characters and hyphens, and cannot start or end with a hyphen. Use '*' to replace any pending request."` | |
| 011 | **Non-existent Request ID** | `"replace": "db8c0f73-ac73-4b90-bba4-8a26ae2fcb27"` (random GUID) | `412` | `412001` |
| | | | `"No active certificate request found to replace. Ensure the request ID in the 'replace' property matches an existing pending request."` | |
| | | | `info.requestId`: The provided request ID | |

---

### 6️⃣ Request ID Validation

| # | Scenario | Topic | Response |
|---|----------|-------|----------|
| 014 | **Missing $rid** | `$iothub/credentials/POST/issueCertificate/` (no `?$rid=`) | **No response** (connection may drop) |
| | | Gateway cannot route response without request ID | |

---

### 7️⃣ Happy Path Flow

| # | Scenario | Flow |
|---|----------|------|
| 015 | **Single CSR Request** | |
| | **Step 1**: Publish CSR | `$iothub/credentials/POST/issueCertificate/?$rid=156089087` |
| | **Step 2**: Receive 202 | `{ "correlationId": "...", "operationExpires": "..." }` |
| | **Step 3**: Receive 200 | `{ "correlationId": "...", "certificates": ["<leaf>", "<intermediate>", "<root>"] }` |

---

### 8️⃣ Subscription Behavior

| # | Scenario | Expected Behavior |
|---|----------|-------------------|
| 016 | **Never Subscribe** | No response received (publish without subscribing) |
| 017 | **Unsubscribe After 202** | Certificate not delivered (200 never received) |
| 018 | **Resubscribe After Unsubscribe** | Certificate delivered after resubscribing |
| 019 | **Reconnect with `clean_session=false`** | Certificate delivered (session persisted) |
| 020 | **Reconnect with `clean_session=true` + Resubscribe** | Certificate delivered |
| 021 | **Reconnect with `clean_session=true` (no resubscribe)** | No certificate delivered |
| 026 | **Send Before Subscribe** | No response (connection may drop) |

---

### 9️⃣ Concurrency & Conflict Handling

| # | Scenario | Behavior |
|---|----------|----------|
| 022 | **Sequential Requests** | Both succeed: first completes (`202`→`200`), then second starts |
| 023 | **Concurrent Requests** | One succeeds (`202`→`200`), other gets `409` conflict |
| 024 | **Request After 202 (no replace)** | Second gets `409` conflict |
| 025 | **Request with `replace=<rid>`** | Cancels pending, new request proceeds |
| 025 | **Request with `replace=*`** | Cancels any pending, new request proceeds |

### Conflict Response (409)
```json
{
  "errorCode": 409004,
  "message": "A credential management operation is already active. Use the requestId in info to check the status of the existing request, or send a new request with 'replace' to cancel and start a new one.",
  "trackingId": "...",
  "timestampUtc": "...",
  "info": {
    "requestId": "304937152",
    "operationExpires": "2026-01-31T12:56:08.892000000Z",
    "correlationId": "418fb379-71ad-4e84-9f47-3795f871cebd"
  }
}
```

---

## 📊 Error Code Reference

| Code | HTTP Status | Meaning |
|------|-------------|---------|
| `400004` | 400 | Invalid request payload (missing/invalid field) |
| `400006` | 400 | Malformed JSON syntax |
| `400037` | 400 | Backend CSR validation failed |
| `409004` | 409 | Conflict - another request is active |
| `412001` | 412 | Precondition failed - no matching request to replace |

---

## ✅ SDK Implementation Checklist

- [ ] Subscribe to `$iothub/credentials/res/#` before publishing
- [ ] Always include `?$rid=<unique_id>` in request topic
- [ ] Match `id` field to authenticated device identity
- [ ] Base64-encode CSR and keep under 8KB
- [ ] Only include `id`, `csr`, and optionally `replace` fields
- [ ] Handle `202` as "pending" - wait for final `200` or error
- [ ] Handle `409` by using `replace` field or waiting for current request
- [ ] Maintain subscription or use `clean_session=false` for reconnects
- [ ] Store `correlationId` for tracking/support

---

## 📝 Example SDK Flow

```
1. CONNECT (x509, clean_session=true)
2. SUBSCRIBE "$iothub/credentials/res/#"
3. PUBLISH "$iothub/credentials/POST/issueCertificate/?$rid=12345"
   Payload: {"id": "my-device", "csr": "MIHj...", "replace": "*"}
4. RECEIVE "$iothub/credentials/res/202/?$rid=12345"
   → Store correlationId, continue listening
5. RECEIVE "$iothub/credentials/res/200/?$rid=12345"
   → Parse certificates array, update device certificate
```

---

*Generated from test execution: 2026-01-30 | 25/26 tests passed (96.2%)*