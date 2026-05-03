const runtimeConfig = (globalThis as {
  __LUMINAPATH_CONFIG__?: {
    apiEndpoint?: string;
  };
}).__LUMINAPATH_CONFIG__;

export const environment = {
  production: true,
  endpoint: runtimeConfig?.apiEndpoint ?? '/api',
};
