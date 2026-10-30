# Storage Providers and Migration

LuminaPath supports multiple storage providers for user-uploaded media (covers, documents, avatars) and automated database backup archives:

- **FileSystem** (default, local container volume)
- **AWS S3** / S3-compatible object storage (MinIO, Cloudflare R2, Wasabi, etc.)
- **Google Drive** (private files in a personal Google Drive account)
- **Azure Blob Storage**

This runbook covers configuring each provider, managing credentials, and executing zero-downtime migrations between storage backends.

---

## Storage Architecture & Volume Structure

All uploads and sensitive application state live under `/app/App_Data` inside the API container, backed by the named Docker volume `luminapath-data`:

```text
/app/App_Data/
├── storage/        # Local FileSystem uploads and pg_dump database backups
├── logs/           # Rolling application logs (Serilog)
├── connections/    # OAuth tokens & provider connection state (Google Drive)
└── keys/           # ASP.NET Data Protection keys (cookies, session encryption)
```

> [!IMPORTANT]
> Always back up the `luminapath-data` volume alongside your PostgreSQL database. The `keys/` directory contains ASP.NET Data Protection keys needed to decrypt stored Google Drive tokens, Google Calendar refresh tokens, and session cookies.

Files are served privately through authenticated API endpoints (`/api/files/...`). Even when using cloud storage, buckets and Drive files remain private and are not directly exposed to the internet.

---

## Provider Configuration

### 1. FileSystem Storage (Default)

FileSystem storage stores files on local disk within the `luminapath-data` volume. It requires zero configuration.

| Variable | Default | Description |
|---|---|---|
| `Storage__Provider` | `FileSystem` | Active storage provider. |
| `Storage__Path` | `App_Data/storage` | Internal filesystem path for uploads. |

---

### 2. AWS S3 and S3-Compatible Storage

Select `S3` to store uploads in an existing private AWS S3 bucket or compatible S3 object storage (MinIO, Cloudflare R2, Wasabi, Ceph). LuminaPath does not create buckets or alter ACLs automatically; create your private bucket beforehand.

#### AWS S3 Example (`.env`)

```dotenv
Storage__Provider=S3
S3__BucketName=my-luminapath-media
S3__Region=eu-central-1
AWS_ACCESS_KEY_ID=AKIAIOSFODNN7EXAMPLE
AWS_SECRET_ACCESS_KEY=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY
```

Authentication uses the AWS SDK credential chain by default (IAM instance roles, ECS task roles, or environment variables).

#### S3-Compatible Service Example (MinIO / Cloudflare R2 / Wasabi)

```dotenv
Storage__Provider=S3
S3__BucketName=my-luminapath-media
S3__Region=us-east-1
S3__ServiceUrl=https://s3.example.com
S3__ForcePathStyle=true
S3__AccessKeyId=minioadmin
S3__SecretAccessKey=miniopassword
```

#### S3 Configuration Reference

| Variable | Default | Description |
|---|---|---|
| `S3__BucketName` | empty | Required. S3 bucket name. |
| `S3__Region` | `us-east-1` | AWS region or compatible service signing region. |
| `S3__ServiceUrl` | empty | API endpoint for S3-compatible providers (omit for AWS S3). |
| `S3__ForcePathStyle` | `false` | Enable for MinIO or services requiring path-style addressing (`endpoint/bucket`). |
| `S3__Prefix` | `luminapath/` | Root prefix for objects within the bucket. |
| `S3__AccessKeyId` / `S3__AccessKeyIdFile` | empty | Explicit access key or mounted Docker secret file. |
| `S3__SecretAccessKey` / `S3__SecretAccessKeyFile` | empty | Explicit secret key or mounted Docker secret file. |
| `S3__SessionToken` / `S3__SessionTokenFile` | empty | Optional session token for temporary credentials. |
| `S3__UploadBackups` | `true` | Copy completed PostgreSQL dumps to `luminapath/backups/`. |

#### IAM Permissions

The IAM user or role requires permissions scoped to your installation prefix:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:ListBucket"
      ],
      "Resource": "arn:aws:s3:::my-luminapath-media",
      "Condition": {
        "StringLike": {
          "s3:prefix": ["luminapath/*"]
        }
      }
    },
    {
      "Effect": "Allow",
      "Action": [
        "s3:GetObject",
        "s3:PutObject",
        "s3:DeleteObject",
        "s3:AbortMultipartUpload"
      ],
      "Resource": "arn:aws:s3:::my-luminapath-media/luminapath/*"
    }
  ]
}
```

> [!TIP]
> Files larger than 8 MiB automatically use multi-part uploads with bounded memory, supporting files up to 5 TiB. Configure an S3 lifecycle rule to expire incomplete multipart uploads after 7 days.

---

### 3. Google Drive Storage

Store uploads and database backups privately in your personal Google Drive account without exposing them publicly.

#### Browser-Based OAuth Setup (Recommended)

1. Open the [Google Cloud Console](https://console.cloud.google.com/) and enable the **Google Drive API**.
2. Create an **OAuth 2.0 Web Application** credential:
   - Name: `LuminaPath Storage`
   - Authorized Redirect URI: `https://lumina.example.com/api/integrations/google-drive/callback`
   *(For local testing, use `http://localhost:4200/api/integrations/google-drive/callback`)*
3. Add the OAuth client settings to `.env`:

   ```dotenv
   GoogleDrive__ClientId=your-client-id.apps.googleusercontent.com
   GoogleDrive__ClientSecret=your-client-secret
   GoogleDrive__RedirectUri=https://lumina.example.com/api/integrations/google-drive/callback
   ```

4. Restart the API container:
   ```sh
   docker compose up -d api
   ```
5. Sign in as an administrator, go to **Admin Overview → Storage** (`/Admin/Overview?tab=storage`), and click **Connect Google Drive**.
6. Follow Google's consent screen. LuminaPath securely stores an encrypted offline token under `App_Data/connections/google-drive/`.
7. Once connected, update `.env` to set `Storage__Provider=GoogleDrive` and restart `docker compose up -d`.

#### Headless / Python Helper Script

For headless servers without a browser callback URL, generate the token on your workstation:

```sh
python3 scripts/authorize-google-drive.py --client-json /path/to/client.json
```

This generates `client_id.txt`, `client_secret.txt`, and `refresh_token.txt` in `secrets/google-drive/`. These can be mounted into Docker via `compose.override.yml`:

```yaml
services:
  api:
    secrets:
      - google_drive_client_secret
      - google_drive_refresh_token
secrets:
  google_drive_client_secret:
    file: ./secrets/google-drive/client_secret.txt
  google_drive_refresh_token:
    file: ./secrets/google-drive/refresh_token.txt
```

And in `.env`:

```dotenv
Storage__Provider=GoogleDrive
GoogleDrive__ClientId=your-client-id.apps.googleusercontent.com
GoogleDrive__ClientSecretFile=/run/secrets/google_drive_client_secret
GoogleDrive__RefreshTokenFile=/run/secrets/google_drive_refresh_token
```

#### Google Drive Configuration Reference

| Variable | Default | Description |
|---|---|---|
| `GoogleDrive__ClientId` | empty | OAuth 2.0 Client ID. |
| `GoogleDrive__ClientSecret` / `GoogleDrive__ClientSecretFile` | empty | OAuth Client Secret or mounted secret file. |
| `GoogleDrive__RefreshToken` / `GoogleDrive__RefreshTokenFile` | empty | Refresh token for headless setup (omit when using browser OAuth). |
| `GoogleDrive__RedirectUri` | empty | Registered redirect URI for web OAuth flow. |
| `GoogleDrive__FolderName` | `LuminaPath` | Root folder name created in Google Drive. |
| `GoogleDrive__FolderId` | empty | Optional existing folder ID to use as root. |
| `GoogleDrive__UploadBackups` | `true` | Copy automated database dumps to the Drive `backups` folder. |
| `GoogleDrive__CacheMinutes` | `5` | In-memory upload cache lifetime (up to 4 MiB per file, max 32 MiB total). |

---

### 4. Azure Blob Storage

| Variable | Default | Description |
|---|---|---|
| `Storage__Provider` | `Azure` | Set to `Azure`. |
| `Azure__BlobConnectionString` | empty | Azure Storage connection string. |
| `Azure__BlobContainerName` | `media` | Container name for uploads. |

---

## Switching Storage Providers & Migrations

LuminaPath provides a built-in migration tool to copy uploaded documents and cover art from one provider to another with cryptographic verification.

### Zero-Downtime Fallback (`Storage:FallbackProvider`)

When switching providers, you can configure a fallback provider in `.env`:

```dotenv
Storage__Provider=S3
Storage__FallbackProvider=FileSystem
```

With this configuration:
- All **new uploads** write to `S3` (primary).
- Any **read request** for an older file not yet migrated to `S3` automatically checks `FileSystem` (fallback).
- This ensures zero user-facing 404s while you migrate existing files in the background!

---

### Step-by-Step Migration Runbook

#### 1. Configure Credentials

Ensure `.env` contains valid credentials for both the **source** and **destination** providers. If migrating to or from Google Drive, ensure browser authorization was completed first.

#### 2. Stop Traffic

Stop the frontend and API containers so no new uploads occur during the copy phase:

```sh
docker compose stop api frontend
```

#### 3. Run a Dry Run

Preview the files to be migrated without modifying the destination:

```sh
# Example: Migrate from FileSystem to S3
docker compose run --rm --no-deps api \
  --migrate-storage=FileSystem \
  --to-storage=S3 \
  --dry-run=true
```

Review the output: it reports total files, total bytes, and any conflicts.

#### 4. Execute the Migration

Run the migration command with `--dry-run=false`:

```sh
docker compose run --rm --no-deps api \
  --migrate-storage=FileSystem \
  --to-storage=S3 \
  --dry-run=false
```

How the migration executes:
1. Copies each blob from the source to the destination with stream piping (bounded memory).
2. Computes the SHA-256 hash of the copied file and verifies integrity.
3. Automatically skips files that already exist on the destination with matching size and checksum (resumable).
4. Does **not** delete source files.

#### 5. Update Configuration and Restart

Update `.env` with the new provider:

```dotenv
Storage__Provider=S3
# Optionally keep fallback while verifying:
Storage__FallbackProvider=FileSystem
```

Restart the stack:

```sh
docker compose up -d
```

---

## Storage Reconciliation & Health Audits

LuminaPath includes audit utilities available in the web interface at **Admin Overview → Storage** (`/Admin/Overview?tab=storage`).

### Audit Capabilities

- **Ghost Blobs Detection**: Identifies document records in PostgreSQL whose underlying files are missing from the active storage provider.
- **Orphan Blobs Detection**: Identifies physical files in storage that do not correspond to any active document record in the database.
- **Automated Orphan Cleanup**: The background service `OrphanedBlobCleanupService` runs every 24 hours to delete unreferenced blobs that have been orphaned for at least 24 hours (grace period prevents deleting files during mid-flight uploads).

---

## When It Goes Wrong

| Symptom | Likely Cause | Solution |
|---|---|---|
| `Storage migration failed: Source file not found` | File was deleted from source storage manually | Verify source directory contents. If the file is unrecoverable, remove the database record or recreate the placeholder. |
| `S3: Access Denied (403)` | Missing IAM permissions for prefix or bucket | Ensure the IAM policy includes `s3:ListBucket`, `s3:GetObject`, `s3:PutObject`, and `s3:AbortMultipartUpload` for the bucket and prefix. |
| `Google Drive token expired after 7 days` | OAuth consent screen is set to "Testing" in Google Cloud | Switch the OAuth consent screen to "In Production" in Google Cloud Console, or re-authorize in Admin Overview → Storage. |
| `Checksum mismatch during migration` | File was modified or corrupted in transit | Rerun the migration command. LuminaPath rejects corrupted files and retains the source copy. |
| `Google Drive redirect URI mismatch` | Hostname or scheme changed behind reverse proxy | Ensure `GoogleDrive__RedirectUri` exactly matches the URI registered in Google Cloud Console. |
