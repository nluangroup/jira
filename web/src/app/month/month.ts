import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-month',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<h1>My month</h1><p>Coming soon.</p>`,
})
export class Month {}
