const detailRoutePatterns = [
  /^\/auth(?:\/|$)/,
  /^\/media\/[^/?#]+(?:[/?#]|$)/,
  /^\/chat\/[^/?#]+(?:[/?#]|$)/,
];

export function shouldHideAppNavigation(url: string): boolean {
  const path = (url || '').split(/[?#]/)[0] || '/';
  return detailRoutePatterns.some((pattern) => pattern.test(path));
}
