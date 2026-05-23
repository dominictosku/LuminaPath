import {
  authenticatorQrSvg,
  buildAuthenticatorUri,
  formatSharedKey,
} from './authenticator-qr';

describe('buildAuthenticatorUri', () => {
  it('embeds issuer + account label + secret + standard TOTP params', () => {
    const uri = buildAuthenticatorUri('alice@example.test', 'JBSWY3DPEHPK3PXP');

    expect(uri.startsWith('otpauth://totp/LuminaPath:alice%40example.test?')).toBeTrue();
    expect(uri).toContain('secret=JBSWY3DPEHPK3PXP');
    expect(uri).toContain('issuer=LuminaPath');
    expect(uri).toContain('algorithm=SHA1');
    expect(uri).toContain('digits=6');
    expect(uri).toContain('period=30');
  });

  it('substitutes "user" for empty account labels so the otpauth URI stays valid', () => {
    const uri = buildAuthenticatorUri('', 'JBSWY3DPEHPK3PXP');
    expect(uri).toContain('LuminaPath:user');
  });

  it('url-encodes account labels with reserved characters', () => {
    const uri = buildAuthenticatorUri('a b@example.test', 'JBSWY3DPEHPK3PXP');
    expect(uri).toContain('a%20b%40example.test');
  });
});

describe('formatSharedKey', () => {
  it('uppercases and groups in fours', () => {
    expect(formatSharedKey('jbswy3dpehpk3pxp')).toBe('JBSW Y3DP EHPK 3PXP');
  });

  it('handles a key whose length is not a multiple of four', () => {
    expect(formatSharedKey('JBSWY3DPEH')).toBe('JBSW Y3DP EH');
  });

  it('strips ambient whitespace before formatting', () => {
    expect(formatSharedKey('  JB SW Y3 DP  ')).toBe('JBSW Y3DP');
  });
});

describe('authenticatorQrSvg', () => {
  it('returns an SVG markup string for a non-trivial URI', () => {
    const svg = authenticatorQrSvg('otpauth://totp/test?secret=JBSWY3DPEHPK3PXP');
    expect(svg.startsWith('<svg')).toBeTrue();
    expect(svg).toContain('</svg>');
  });
});
