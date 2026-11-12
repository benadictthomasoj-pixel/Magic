import { TestBed } from '@angular/core/testing';
import { IntroStateService } from '../../../core/services/intro-state.service';
import { BrandIntroComponent } from './brand-intro.component';

describe('BrandIntroComponent', () => {
  let introState: IntroStateService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BrandIntroComponent],
    }).compileComponents();

    introState = TestBed.inject(IntroStateService);
    introState.resetForSession();
  });

  it('renders official brand logo image without distortion', () => {
    const fixture = TestBed.createComponent(BrandIntroComponent);
    fixture.detectChanges();

    const logo = fixture.nativeElement.querySelector('.brand-logo') as HTMLImageElement;
    expect(logo).toBeTruthy();
    expect(logo.getAttribute('src')).toBe('/branding/magic-logo.jpg');
    expect(logo.getAttribute('alt')).toContain('MAGIC');
  });

  it('skips intro and marks completed when skip button is clicked', () => {
    const fixture = TestBed.createComponent(BrandIntroComponent);
    const component = fixture.componentInstance;
    let completedEmitted = false;
    component.completed.subscribe(() => {
      completedEmitted = true;
    });

    fixture.detectChanges();
    const skipBtn = fixture.nativeElement.querySelector('.skip-button') as HTMLButtonElement;
    skipBtn.click();

    expect(completedEmitted).toBe(true);
    expect(introState.hasCompletedIntro()).toBe(true);
  });
});
