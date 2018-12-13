import { createUserManager } from 'redux-oidc';

const userManagerConfig = {
  client_id: 'js',
  redirect_uri: 'http://localhost:3000/callback',
  response_type: 'token id_token',
  scope:"openid profile",
  authority: 'https://localhost:44393/',
  automaticSilentRenew: true,
  filterProtocolClaims: true,
  loadUserInfo: true,
  monitorSession: true
};

const userManager = createUserManager(userManagerConfig);

export default userManager;