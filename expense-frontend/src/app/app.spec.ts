import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { Subject } from 'rxjs';
import { App } from './app';
import { AuthService } from './shared/services/auth.service';
import { ErrorService } from './shared/services/error.service';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        provideAnimations(),
        { provide: AuthService, useValue: { isAuthenticated: vi.fn().mockReturnValue(false) } },
        { provide: ErrorService, useValue: { error$: new Subject() } },
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    expect(fixture.componentInstance).toBeTruthy();
  });
});
