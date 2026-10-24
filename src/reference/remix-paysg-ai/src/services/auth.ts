export interface User {
  uid: string;
  email: string | null;
  displayName: string | null;
  photoURL?: string | null;
}

// Global window declaration for Google Identity Services (GIS)
declare global {
  interface Window {
    google?: any;
  }
}

let cachedAccessToken: string | null = null;
let cachedUser: User | null = null;
const authListeners: Array<(user: User | null, token: string | null) => void> = [];

export const auth = null;

export const initAuth = (
  onAuthSuccess?: (user: User, token: string) => void,
  onAuthFailure?: () => void
) => {
  const listener = (user: User | null, token: string | null) => {
    if (user && token) {
      if (onAuthSuccess) onAuthSuccess(user, token);
    } else {
      if (onAuthFailure) onAuthFailure();
    }
  };

  authListeners.push(listener);

  // Check persisted state in localStorage
  try {
    const isLoggedOut = localStorage.getItem('paysg_explicitly_logged_out') === 'true';
    const savedToken = localStorage.getItem('paysg_token');
    const savedUserJson = localStorage.getItem('paysg_user');

    if (savedToken && savedUserJson && !isLoggedOut) {
      const parsedUser = JSON.parse(savedUserJson);
      cachedAccessToken = savedToken;
      cachedUser = parsedUser;
      if (onAuthSuccess) {
        onAuthSuccess(parsedUser, savedToken);
      }
    } else if (onAuthFailure) {
      onAuthFailure();
    }
  } catch (e) {
    if (onAuthFailure) onAuthFailure();
  }

  return () => {
    const idx = authListeners.indexOf(listener);
    if (idx !== -1) {
      authListeners.splice(idx, 1);
    }
  };
};

export const requestGoogleIdentityAccessToken = async (
  emailHint = 'smu.ilp.team5@gmail.com'
): Promise<{ user: User; accessToken: string }> => {
  return new Promise((resolve, reject) => {
    if (!window.google?.accounts?.oauth2) {
      return reject(new Error('Google Identity Services client is still loading. Please try again.'));
    }

    try {
      const client = window.google.accounts.oauth2.initTokenClient({
        client_id: '714400668672-smupaysg.apps.googleusercontent.com',
        scope:
          'https://www.googleapis.com/auth/gmail.readonly https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/userinfo.profile',
        hint: emailHint,
        callback: async (tokenResponse: any) => {
          if (tokenResponse.error) {
            return reject(new Error(tokenResponse.error_description || tokenResponse.error));
          }

          const token = tokenResponse.access_token;
          cachedAccessToken = token;

          let profileUser: User = {
            email: emailHint,
            displayName: 'SMU ILP Team 5',
            photoURL: '',
            uid: `gis-${Date.now()}`,
          };

          try {
            const userinfoRes = await fetch('https://www.googleapis.com/oauth2/v3/userinfo', {
              headers: { Authorization: `Bearer ${token}` },
            });
            if (userinfoRes.ok) {
              const u = await userinfoRes.json();
              profileUser = {
                email: u.email || emailHint,
                displayName: u.name || 'SMU ILP Team 5',
                photoURL: u.picture || '',
                uid: u.sub || `gis-${Date.now()}`,
              };
            }
          } catch (e) {
            console.warn('Could not fetch userinfo, using default profile info:', e);
          }

          cachedUser = profileUser;
          authListeners.forEach((fn) => fn(profileUser, token));

          resolve({
            user: profileUser,
            accessToken: token,
          });
        },
      });

      client.requestAccessToken({ prompt: 'consent' });
    } catch (err: any) {
      reject(err);
    }
  });
};

export const googleSignIn = async (
  preferredEmail = 'smu.ilp.team5@gmail.com'
): Promise<{ user: User; accessToken: string } | null> => {
  // If Google Identity Services (GIS) client is loaded on the page, request live token
  if (window.google?.accounts?.oauth2) {
    try {
      const gisResult = await requestGoogleIdentityAccessToken(preferredEmail);
      return gisResult;
    } catch (gisErr: any) {
      console.warn('Google Identity Services popup failed or was cancelled:', gisErr);
      throw gisErr;
    }
  }

  // Instant fallback to default AP mailbox session
  const defaultUser: User = {
    email: preferredEmail,
    displayName: 'SMU ILP Team 5 (AP Finance Lead)',
    photoURL: '',
    uid: 'ap-mailbox-smu-ilp-team5',
  };
  const defaultToken = 'ap-default-mailbox-token';
  cachedAccessToken = defaultToken;
  cachedUser = defaultUser;
  authListeners.forEach((fn) => fn(defaultUser, defaultToken));
  return { user: defaultUser, accessToken: defaultToken };
};

export const getAccessToken = async (): Promise<string | null> => {
  return cachedAccessToken || localStorage.getItem('paysg_token');
};

export const setCachedToken = (token: string | null) => {
  cachedAccessToken = token;
};

export const logout = async () => {
  cachedAccessToken = null;
  cachedUser = null;
  authListeners.forEach((fn) => fn(null, null));
};

