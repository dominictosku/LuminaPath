import qrcodeGenerator from 'qrcode-generator';

const ISSUER = 'LuminaPath';
const ALGORITHM = 'SHA1';
const DIGITS = 6;
const PERIOD = 30;

/**
 * Builds the standard otpauth:// URI an authenticator app expects.
 * `accountLabel` is typically the user's email so the entry is
 * recognisable in the app.
 */
export function buildAuthenticatorUri(accountLabel: string, sharedKey: string): string {
  const issuer = encodeURIComponent(ISSUER);
  const label = `${issuer}:${encodeURIComponent(accountLabel || 'user')}`;
  const params = new URLSearchParams({
    secret: sharedKey,
    issuer: ISSUER,
    algorithm: ALGORITHM,
    digits: String(DIGITS),
    period: String(PERIOD),
  });
  return `otpauth://totp/${label}?${params.toString()}`;
}

/**
 * Returns an SVG markup string for the given otpauth URI. Trusted
 * (we generate it ourselves) so the caller can drop it into
 * [innerHTML] after passing through DomSanitizer.bypassSecurityTrustHtml.
 *
 * Type-correction level "M" balances density vs. error resilience for a
 * 100-200 char otpauth URI on a small screen.
 */
export function authenticatorQrSvg(otpauthUri: string): string {
  const qr = qrcodeGenerator(0, 'M');
  qr.addData(otpauthUri);
  qr.make();
  // 4-pixel module size + 0 margin keeps the SVG dense enough to scan
  // on a 200-260px tile without bloating the markup.
  return qr.createSvgTag({ cellSize: 4, margin: 0, scalable: true });
}

/**
 * Pretty-prints the raw base32 secret as space-separated groups of 4
 * so users typing it into an authenticator app by hand have an easier
 * time. e.g. "JBSW Y3DP EHPK 3PXP".
 */
export function formatSharedKey(sharedKey: string): string {
  const clean = sharedKey.replace(/\s+/g, '').toUpperCase();
  const groups: string[] = [];
  for (let i = 0; i < clean.length; i += 4) {
    groups.push(clean.slice(i, i + 4));
  }
  return groups.join(' ');
}
