import { AfterViewInit, Component, ElementRef, OnInit, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Capacitor } from '@capacitor/core';
import { SocialLogin } from '@capgo/capacitor-social-login';
import { Auth } from '../../core/auth';
import { AppConfig } from '../../core/app-config';
import { loadGoogleIdentityScript } from '../../core/google-identity';

declare const google: any;

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login implements OnInit, AfterViewInit {
  mode = signal<'login' | 'register'>('login');
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  pendingGoogleIdToken = signal<string | null>(null);
  showPassword = signal(false);

  email = '';
  password = '';
  organizationName = '';
  displayName = '';

  private readonly googleButton = viewChild<ElementRef<HTMLDivElement>>('googleButton');
  private googleReady = false;
  private viewReady = false;

  // Android's embedded WebView is blocked from using Google's web-based Identity
  // Services widget (see the comment in capacitor.config.ts), so on native platforms
  // this renders a plain button that goes through the native Credential Manager flow
  // instead of the GSI script/button used on web.
  readonly isNative = Capacitor.isNativePlatform();
  private googleNativeInitialized = false;

  constructor(
    private readonly auth: Auth,
    private readonly router: Router,
    private readonly appConfig: AppConfig,
  ) {}

  get googleClientId(): string | null {
    return this.appConfig.googleClientId;
  }

  ngOnInit(): void {
    if (!this.googleClientId) {
      return;
    }

    if (this.isNative) {
      // webClientId, not an Android client -- Credential Manager issues ID tokens
      // whose audience matches the Web client ID, which is what the backend already
      // validates (same client ID the web GSI flow below uses). The separate Android
      // OAuth client registered in Google Cloud Console (package name + SHA-1) never
      // appears in app code; it just has to exist for Credential Manager to authorize
      // this app.
      SocialLogin.initialize({ google: { webClientId: this.googleClientId } })
        .then(() => {
          this.googleNativeInitialized = true;
        })
        .catch(() => {
          // No Google button rather than a broken page.
        });
      return;
    }

    loadGoogleIdentityScript()
      .then(() => {
        google.accounts.id.initialize({
          client_id: this.googleClientId,
          callback: (response: { credential: string }) => this.onGoogleCredential(response.credential),
        });
        this.googleReady = true;
        this.renderGoogleButtonIfReady();
      })
      .catch(() => {
        // No Google button rather than a broken page -- everything else about
        // login/register works fine without it.
      });
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.renderGoogleButtonIfReady();
  }

  private renderGoogleButtonIfReady(): void {
    if (!this.googleReady || !this.viewReady) {
      return;
    }
    const el = this.googleButton()?.nativeElement;
    if (el) {
      google.accounts.id.renderButton(el, { theme: 'outline', size: 'large', width: 320 });
    }
  }

  toggleMode(): void {
    this.mode.set(this.mode() === 'login' ? 'register' : 'login');
    this.errorMessage.set(null);
  }

  submit(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const request =
      this.mode() === 'login'
        ? this.auth.login(this.email, this.password)
        : this.auth.register(this.organizationName, this.email, this.password, this.displayName);

    request.subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigateByUrl('/dashboard');
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Something went wrong. Check your details and try again.');
      },
    });
  }

  async signInWithGoogleNative(): Promise<void> {
    if (!this.googleNativeInitialized) {
      return;
    }
    this.errorMessage.set(null);
    try {
      const { result } = await SocialLogin.login({ provider: 'google', options: { scopes: ['email', 'profile'] } });
      const idToken = (result as { idToken?: string | null } | undefined)?.idToken;
      if (!idToken) {
        this.errorMessage.set('Could not sign in with Google.');
        return;
      }
      this.onGoogleCredential(idToken);
    } catch {
      // User cancelled the native sign-in sheet, or it failed -- leave the form as-is.
    }
  }

  private onGoogleCredential(idToken: string): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.auth.googleSignIn(idToken).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigateByUrl('/dashboard');
      },
      error: (err) => {
        this.loading.set(false);
        if (err?.error?.requiresOrganizationName) {
          this.organizationName = '';
          this.pendingGoogleIdToken.set(idToken);
        } else {
          this.errorMessage.set(err?.error?.message ?? 'Could not sign in with Google.');
        }
      },
    });
  }

  continueGoogleSignUp(): void {
    const idToken = this.pendingGoogleIdToken();
    if (!idToken) {
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    this.auth.googleSignIn(idToken, this.organizationName).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigateByUrl('/dashboard');
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Could not finish creating your account.');
      },
    });
  }

  cancelGoogleSignUp(): void {
    this.pendingGoogleIdToken.set(null);
    this.errorMessage.set(null);
  }
}
