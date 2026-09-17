import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Auth } from '../../core/auth';

// Public route (outside authGuard) that knightage-platform's service launcher redirects to
// with ?token=<jwt> after the user picks CRM from their list of services. Decodes and
// stores the token the same way a direct login does, then routes into the app.
@Component({
  selector: 'app-auth-callback',
  imports: [],
  templateUrl: './auth-callback.html',
})
export class AuthCallback implements OnInit {
  errorMessage: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly auth: Auth,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.errorMessage = 'No token was provided in the redirect.';
      return;
    }

    try {
      this.auth.setTokenFromCallback(token);
      this.router.navigateByUrl('/');
    } catch {
      this.errorMessage = 'Could not complete sign-in with the provided token.';
    }
  }
}
